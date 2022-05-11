using System;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

internal sealed class ExternalApp : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ExternalApp> _logger;
    public ApplicationState ApplicationState { get; private set; } = ApplicationState.Stopped;
    public ExternalAppConfiguration Configuration { get; }
    public bool HasWindow => WindowHandle.Value != IntPtr.Zero;
    public bool IsEmbedded { get; set; }
    public bool IsRunning => Process is {HasExited: false};
    public Process? Process { get; private set; }
    public HWND WindowHandle { get; private set; }
    public event EventHandler? ProcessExited;

    public ExternalApp(ExternalAppConfiguration configuration, ILoggerFactory loggerFactory)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        _logger = loggerFactory.CreateLogger<ExternalApp>();
    }

    public void Dispose()
    {
    }

    public async Task<NativeResult> StartAsync(CancellationToken cancellationToken)
    {
        if (ApplicationState != ApplicationState.Stopped)
            throw new InvalidOperationException("Cannot start application because it is already starting or running.");

        try
        {
            ApplicationState = ApplicationState.Starting;
            
            var result = await StartApplicationInternalAsync(cancellationToken);

            ApplicationState = result.Succeeded
                ? ApplicationState.Running
                : ApplicationState.Stopped;

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{Method} has been cancelled", nameof(StartAsync));
            ApplicationState = ApplicationState.Stopped;
            
            return NativeResult.Fail();
        }
    }

    private async Task<NativeResult> StartApplicationInternalAsync(CancellationToken cancellationToken)
    {
        #region --- Start Process (if needed) ---

        var result = await StartProcessAsync(cancellationToken);
        if (!result.Succeeded)
            return result;

        #endregion

        #region --- Search for a specific process or command line (if configured) ---

        result = await FindProcessAsync(cancellationToken);
        if (!result.Succeeded)
            _logger.LogWarning(result.Exception, "FindProcessAsync raised an error");

        #endregion

        #region --- Search for a specific window title (if configured) ---

        if (!string.IsNullOrEmpty(Configuration.WindowTitleMatch))
        {
            var windowFound = await FindWindowTitleAsync(cancellationToken);
            if (!windowFound)
            {
                // TODO: implement event to ask which window to use
                //ExternalAppState.WindowHandle = GetWindowHandleFromPicker();
                if (!HasWindow)
                {
                    return NativeResult.Fail();
                }
            }
        }

        #endregion

        if (Process == null)
            return NativeResult.Fail();

        Process.EnableRaisingEvents = true;
        Process.Exited += AppProcess_Exited;

        return NativeResult.Success;
    }

    private async Task<NativeResult> StartProcessAsync(CancellationToken cancellationToken)
    {
        if (Configuration.UseExistingProcess)
            return NativeResult.Success;

        #region --- Prepare Process for Execution and Start ---

        try
        {
            StartProcess();
        }
        catch (Exception ex)
        {
            return NativeResult.Fail(ex);
        }

        if (Process == null)
        {
            return NativeResult.Fail();
        }

        try
        {
            Process.WaitForInputIdle();
        }
        catch
        {
            // ignore: PowerShell doesn't allow to wait for input idle
        }

        if (!IsRunning)
        {
            return NativeResult.Fail(new Exception("The process is not running anymore or does not have a main window handle."));
        }

        #endregion

        #region --- Wait for the App to be started, try to get the main window handle ---

        // depending on the configuration, we don't always need the main window handle of the process we started

        Exception? lastException = null;

        var tryCount = 0;
        var mainWindowHandle = IntPtr.Zero;
        var mainWindowTitle = "Default IME";
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (tryCount >= Configuration.MaxWaitTime * 4)
                break;

            Process.Refresh();

            await Task.Delay(100, cancellationToken);

            tryCount++;

            try
            {
                mainWindowHandle = Process.MainWindowHandle;
                mainWindowTitle = Process.MainWindowTitle;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        } while (!Process.HasExited && (mainWindowHandle == IntPtr.Zero || mainWindowTitle == "Default IME"));

        if (!IsRunning || lastException != null)
        {
            return NativeResult.Fail(lastException);
        }

        WindowHandle = new HWND(Process.MainWindowHandle);

        #endregion

        return NativeResult.Success;
    }

    private void StartProcess()
    {
        var processStartInfo = new ProcessStartInfo(
            Configuration.Command ?? string.Empty,
            Configuration.Arguments ?? string.Empty)
        {
            WindowStyle = Configuration.StartHidden && !Configuration.StartExternal
                ? ProcessWindowStyle.Minimized
                : ProcessWindowStyle.Normal,
            CreateNoWindow = Configuration.StartHidden &&
                             !Configuration.StartExternal,
            UseShellExecute = true,
            WorkingDirectory = Configuration.WorkingDirectory ?? ".",
            LoadUserProfile = Configuration.LoadUserProfile
        };

        if (Configuration.RunElevated)
        {
            processStartInfo.UseShellExecute = true;
            processStartInfo.Verb = "runas";
        }
        else if (Configuration.UseCredentials)
        {
            processStartInfo.UseShellExecute = false;
            processStartInfo.Verb = "runas";
            if (Configuration.Username != null)
                processStartInfo.UserName = Configuration.Username;
            if (Configuration.Domain != null)
                processStartInfo.Domain = Configuration.Domain;
            if (Configuration.Password != null)
                processStartInfo.Password = SecureStringExtensions.ConvertToSecureString(Configuration.Password);
        }

        Process = Process.Start(processStartInfo);
    }

    /// <summary>
    /// 
    /// </summary>
    public void CloseApplication()
    {
        if (!HasWindow || !IsRunning)
            return;

        try
        {
            if (Configuration.KillOnClose)
            {
                Process?.Kill();
            }
            else
            {
                // for now we just kill the process
                if (!Process?.CloseMainWindow() ?? true)
                {
                    Process?.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Closing external application failed");
        }
        finally
        {
            ApplicationState = ApplicationState.Stopped;
        }
    }

    public string GetWindowTitle()
    {
        if (Process == null || WindowHandle.Value == IntPtr.Zero)
            return string.Empty;
        try
        {
            var capLength = PInvoke.GetWindowTextLength(WindowHandle);
            var lpString = new PWSTR();
            PInvoke.GetWindowText(WindowHandle, lpString, capLength);
            return lpString.AsSpan().ToString();
        }
        catch
        {
            // ignored
        }

        return string.Empty;
    }

    private async Task<NativeResult> FindProcessAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Configuration.ProcessNameToTrack) &&
            string.IsNullOrWhiteSpace(Configuration.CommandLineMatchString))
        {
            // no need to search for anything, bail...
            return NativeResult.Success;
        }

        var commandFound = false;

        const string win32Processes = "SELECT ProcessId, CommandLine FROM Win32_Process";
        
        // setup WMI
        var wmiQuery = string.IsNullOrWhiteSpace(Configuration.ProcessNameToTrack)
            ? $"{win32Processes} WHERE Name='{Configuration.ProcessNameToTrack}'"
            : win32Processes;

        var searcher = new ManagementObjectSearcher(wmiQuery);
        _logger.LogDebug("WMI query to execute: {WmiQuery}", wmiQuery);

        var debugInfo = new StringBuilder();
        debugInfo.AppendLine($"Process name to track: {Configuration.ProcessNameToTrack}");
        debugInfo.AppendLine($"Command line fragment to look for: {Configuration.CommandLineMatchString}");

        // assign local variables to prevent leaking memory by capturing members in async methods
        var minWaitTimeInMs = Configuration.MinWaitTime * 1000;
        var maxWaitTime = Configuration.MaxWaitTime;
        var commandLineMatchString = Configuration.CommandLineMatchString;

        // first, wait the MinWaitTime, before we look for a process or command line
        await Task.Delay(minWaitTimeInMs, cancellationToken);

        var tryCount = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            foreach (var managementBaseObject in searcher.Get())
            {
                if (managementBaseObject is not ManagementObject wmiWin32Process)
                    continue;

                var commandLine = wmiWin32Process["CommandLine"] != null
                    ? wmiWin32Process["CommandLine"].ToString()
                    : string.Empty;

                var processIdString = wmiWin32Process["ProcessId"] != null
                    ? wmiWin32Process["ProcessId"].ToString()
                    : string.Empty;

                // check if we can parse the process id and if we have a valid command line
                if (!int.TryParse(processIdString, out var processId))
                    continue;

                if (string.IsNullOrWhiteSpace(commandLine) || processId <= -1)
                    continue;

                debugInfo.AppendLine($"Process: {processIdString}, Command Line: {commandLine}");

                // if no command line match string has been provided, take the process we found
                // or in case a command line match string has been provided, check if it matches
                if (!string.IsNullOrWhiteSpace(commandLineMatchString) &&
                    !commandLine.ToLower().Contains(commandLineMatchString!.ToLower()))
                    continue;

                if (Process != null)
                    Process.Exited -= AppProcess_Exited;

                Process = Process.GetProcessById(processId);
                WindowHandle = new HWND(Process.MainWindowHandle);
                commandFound = true;
                break;
            }

            tryCount++;
            await Task.Delay(250, cancellationToken);

            if (commandFound || tryCount >= maxWaitTime * 4)
                break;
        }

        _logger.LogDebug("{Method} {Result} - Details: {Details}", 
            nameof(FindProcessAsync), commandFound ? "succeeded" : "failed", debugInfo);

        return commandFound
            ? NativeResult.Success
            : NativeResult.Fail();
    }

    private async Task<bool> FindWindowTitleAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Configuration.MinWaitTime * 1000, cancellationToken);

        var tryCountMatch = 0;
        var titleMatches = 0;
        var windowFound = false;

        if (Process == null)
            return false;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (tryCountMatch >= Configuration.MaxWaitTime * 4)
                break;

            await Task.Delay(250, cancellationToken);

            var provider = new ProcessWindowProvider(_loggerFactory.CreateLogger<ProcessWindowProvider>());

            foreach (var window in provider.GetProcessWindows())
            {
                if (window.ProcessId != Process.Id)
                    continue;

                // title matches
                if (!window.WindowTitle.Contains(Configuration.WindowTitleMatch))
                    continue;

                // we are still on the same window
                if (Configuration.WindowTitleMatchSkip > 0 && WindowHandle == window.MainWindowHandle.Value)
                    continue;

                // if window title matches should be skipped
                if (titleMatches <= Configuration.WindowTitleMatchSkip)
                {
                    titleMatches++;
                    break;
                }

                // remember the current window handle
                WindowHandle = window.MainWindowHandle;

                // window finally found
                windowFound = true;
                break;
            }

            tryCountMatch++;
        } while (Process.HasExited == false && !windowFound);

        return windowFound;
    }

    private void AppProcess_Exited(object? sender, EventArgs e)
    {
        try
        {
            ProcessExited?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ApplicationClosed event threw an exception");
        }
    }
}