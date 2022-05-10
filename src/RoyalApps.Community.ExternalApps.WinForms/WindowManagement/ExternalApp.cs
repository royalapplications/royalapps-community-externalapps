using System;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

public class ExternalApp : IDisposable
{
    private readonly ILogger _logger;
    public ExternalAppConfiguration Configuration { get; }
    public ApplicationState ApplicationState { get; private set; } = ApplicationState.Stopped;
    public Process? Process { get; set; }
    public ProcessStartInfo? ProcessStartInfo { get; set; }
    public HWND WindowHandle { get; set; }
    public bool IsEmbedded { get; set; }
    public bool HasWindow => WindowHandle.Value != IntPtr.Zero;
    public bool IsRunning => Process is {HasExited: false};

    public event EventHandler ProcessExited;
    
    public ExternalApp(ExternalAppConfiguration configuration, ILogger logger)
    {
        _logger = logger;
        Configuration = configuration;
    }

    public void Dispose()
    {
    }

    public async Task<NativeResult> StartAsync()
    {
        if (ApplicationState != ApplicationState.Stopped)
            throw new InvalidOperationException("Cannot start application because it is already starting or running.");

        ApplicationState = ApplicationState.Starting;

        var result = await StartApplicationInternalAsync();

        ApplicationState = result.Success
            ? ApplicationState.Running
            : ApplicationState.Stopped;
        return result;
    }

    private async Task<NativeResult> StartApplicationInternalAsync()
    {
        #region --- Start Process (if needed) ---

        var result = await StartProcessAsync();
        if (!result.Success)
            return result;

        #endregion

        #region --- Search for a specific process or command line (if configured) ---

        result = await FindProcessAsync();
        if (!result.Success)
            _logger.LogWarning(result.Exception, "FindProcessAsync raised an error");

        result = new NativeResult(false);

        #endregion

        #region --- Search for a specific window title (if configured) ---

        if (!string.IsNullOrEmpty(Configuration.WindowTitleMatch))
        {
            var windowFound = await FindWindowTitleAsync();
            if (!windowFound)
            {
                // TODO: implement event to ask which window to use
                //ExternalAppState.WindowHandle = GetWindowHandleFromPicker();
                if (!HasWindow)
                {
                    return new NativeResult(false);
                }
            }
        }

        #endregion

        if (Process != null)
        {
            Process.EnableRaisingEvents = true;
            Process.Exited += AppProcess_Exited;
        }

        return new NativeResult(true);
    }

    private async Task<NativeResult> StartProcessAsync()
    {
        if (Configuration.UseExistingProcess)
            return new NativeResult(true);

        #region --- Prepare Process for Execution and Start ---

        try
        {
            StartProcess();
        }
        catch (Exception ex)
        {
            return new NativeResult(false, ex);
        }

        if (Process == null)
        {
            return new NativeResult(false);
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
            return new NativeResult(false,
                new Exception("The process is not running anymore or does not have a main window handle."));
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
            if (tryCount >= Configuration.MaxWaitTime * 4)
                break;

            Process.Refresh();

            await Task.Delay(100);

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
        } while (!Process.HasExited &&
                 (mainWindowHandle == IntPtr.Zero || mainWindowTitle == "Default IME"));

        if (!IsRunning || lastException != null)
        {
            return new NativeResult(false, lastException);
        }

        WindowHandle = new HWND(Process.MainWindowHandle);

        #endregion

        return new NativeResult(true);
    }

    private void StartProcess()
    {
        ProcessStartInfo = new ProcessStartInfo(
            Configuration.Command ?? string.Empty,
            Configuration.Arguments ?? string.Empty)
        {
            WindowStyle = Configuration.StartHidden && !Configuration.StartExternal
                ? ProcessWindowStyle.Minimized
                : ProcessWindowStyle.Normal,
            CreateNoWindow = Configuration.StartHidden &&
                             !Configuration.StartExternal,
            UseShellExecute = true,
            WorkingDirectory = Configuration.WorkingDirectory,
            LoadUserProfile = Configuration.LoadUserProfile
        };

        if (Configuration.RunElevated)
        {
            ProcessStartInfo.UseShellExecute = true;
            ProcessStartInfo.Verb = "runas";
        }
        else if (Configuration.UseCredentials)
        {
            ProcessStartInfo.UseShellExecute = false;
            ProcessStartInfo.Verb = "runas";
            ProcessStartInfo.UserName = Configuration.Username;
            ProcessStartInfo.Domain = Configuration.Domain;
            ProcessStartInfo.Password = SecureStringExtensions.ConvertToSecureString(Configuration.Password);
        }

        Process = Process.Start(ProcessStartInfo);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="killProcess"></param>
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
            var pwstr = new PWSTR();
            PInvoke.GetWindowText(WindowHandle, pwstr, capLength);
            return pwstr.AsSpan().ToString();
        }
        catch
        {
            // ignored
        }

        return string.Empty;
    }

    private async Task<NativeResult> FindProcessAsync()
    {
        if (string.IsNullOrWhiteSpace(Configuration.ProcessNameToTrack) &&
            string.IsNullOrWhiteSpace(Configuration.CommandLineMatchString))
        {
            // no need to search for anything, bail...
            return new NativeResult(true);
        }

        var commandFound = false;

        // setup WMI
        var wmiQuery = "SELECT ProcessId, CommandLine FROM Win32_Process";
        if (!string.IsNullOrWhiteSpace(Configuration.ProcessNameToTrack))
            wmiQuery += $" WHERE Name='{Configuration.ProcessNameToTrack}'";
        var searcher = new ManagementObjectSearcher(wmiQuery);
        _logger.LogDebug("WMI query to execute: {WmiQuery}", wmiQuery);

        var debugInfoStringBuilder = new StringBuilder();
        debugInfoStringBuilder.AppendLine($"Process name to track: {Configuration.ProcessNameToTrack}");
        debugInfoStringBuilder.AppendLine(
            $"Command line fragment to look for: {Configuration.CommandLineMatchString}");

        // assign local variables to prevent leaking memory by capturing members in async methods
        var minWaitTimeInMs = Configuration.MinWaitTime * 1000;
        var maxWaitTime = Configuration.MaxWaitTime;
        var commandLineMatchString = Configuration.CommandLineMatchString;

        // first, wait the MinWaitTime, before we look for a process or command line
        await Task.Delay(minWaitTimeInMs);

        var tryCount = 0;
        while (true)
        {
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

                debugInfoStringBuilder.AppendLine($"Process: {processIdString}, Command Line: {commandLine}");

                // if no command line match string has been provided, take the process we found
                // or in case a command line match string has been provided, check if it matches
                if (string.IsNullOrWhiteSpace(commandLineMatchString) ||
                    commandLine.ToLower().Contains(commandLineMatchString!.ToLower()))
                {
                    if (Process != null)
                        Process.Exited -= AppProcess_Exited;
                    Process = Process.GetProcessById(processId);
                    WindowHandle = new HWND(Process.MainWindowHandle);
                    commandFound = true;
                    break;
                }
            }

            tryCount++;
            Thread.Sleep(250);

            if (commandFound || tryCount >= maxWaitTime * 4)
                break;
        }

        if (commandFound)
        {
            _logger.LogDebug(
                "FindProcessAsync succeeded: {Details}",
                debugInfoStringBuilder);
        }
        else
        {
            _logger.LogDebug(
                "FindProcessAsync failed: {Details}",
                debugInfoStringBuilder);
        }

        return commandFound
            ? new NativeResult(true)
            : new NativeResult(false);
    }

    private async Task<bool> FindWindowTitleAsync()
    {
        await Task.Delay(Configuration.MinWaitTime * 1000);

        var tryCountMatch = 0;
        var titleMatches = 0;
        var windowFound = false;

        if (Process == null)
            return false;

        do
        {
            if (tryCountMatch >= Configuration.MaxWaitTime * 4)
                break;

            await Task.Delay(250);

            var provider = new ProcessWindowProvider(_logger);

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