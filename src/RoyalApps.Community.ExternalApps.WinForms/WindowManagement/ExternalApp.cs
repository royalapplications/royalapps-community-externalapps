using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.Extensions;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

/// <summary>
/// Encapsulates an external process.
/// </summary>
internal sealed class ExternalApp : IDisposable
{
    private WINDOW_STYLE _embeddedGwlStyle;
    private WINDOW_STYLE _originalGwlStyle;

    private readonly ILogger<ExternalApp> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public ExternalApp(ExternalAppConfiguration configuration, ILoggerFactory loggerFactory)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        _logger = loggerFactory.CreateLogger<ExternalApp>();
    }

    /// <summary>
    /// Raised when the external application's process exits.
    /// </summary>
    public event EventHandler? ProcessExited;

    /// <summary>
    /// Raised when no window with the matching criteria has been found.
    /// </summary>
    public event EventHandler<QueryWindowEventArgs>? QueryWindow;

    /// <summary>
    /// Gets the current state of the external application.
    /// </summary>
    public ApplicationState ApplicationState { get; private set; } = ApplicationState.Stopped;

    /// <summary>
    /// Gets the configuration for the external application.
    /// </summary>
    public ExternalAppConfiguration Configuration { get; }

    /// <summary>
    /// Gets a value indicating whether a window handle has been set.
    /// </summary>
    public bool HasWindow => !WindowHandle.IsNull;

    /// <summary>
    /// Gets a value indicating whether the external application's process is running.
    /// </summary>
    public bool IsRunning => Process is {HasExited: false};

    /// <summary>
    /// Gets or sets a value indicating whether the external application is embedded or not.
    /// </summary>
    public bool IsEmbedded { get; private set; }

    /// <summary>
    /// Gets the external application's process.
    /// </summary>
    public Process? Process { get; private set; }

    /// <summary>
    /// Gets the external application's window handle.
    /// </summary>
    public HWND WindowHandle { get; private set; }

    /// <summary>
    /// The original window handle which is used to re-parent.
    /// </summary>
    public HWND OriginalWindowHandle { get; private set; }

    /// <summary>
    /// Closes the external application.
    /// </summary>
    public void CloseApplication()
    {
        if (!HasWindow || !IsRunning)
            return;

        try
        {
            if (Process != null)
            {
                Process.Exited -= AppProcess_Exited;
                if (Configuration.KillOnClose)
                {
                    Process.Kill();
                }
                else
                {
                    // for now we just kill the process
                    if (!Process.CloseMainWindow())
                    {
                        Process.Kill();
                    }
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (Process != null)
        {
            Process.Exited -= AppProcess_Exited;
        }
    }

    /// <summary>
    /// Tries to the external application's window title.
    /// </summary>
    /// <returns>A string containing the window title or an empty string, if not found.</returns>
    public string GetWindowTitle()
    {
        if (Process == null || WindowHandle.IsNull)
            return string.Empty;

        try
        {
            var hWnd = WindowHandle;

            // Get text length via WM_GETTEXTLENGTH
            var length = PInvoke.SendMessage(
                hWnd,
                PInvoke.WM_GETTEXTLENGTH,
                wParam: default,
                lParam: default
            );

            if (length == 0)
            {
                hWnd = PInvoke.GetWindow(hWnd, GET_WINDOW_CMD.GW_CHILD);
                length = PInvoke.SendMessage(
                    hWnd,
                    PInvoke.WM_GETTEXTLENGTH,
                    wParam: default,
                    lParam: default
                );

                if (length == 0)
                    return string.Empty;
            }

            var textLength = (int)length.Value;
            if (textLength == 0)
                return string.Empty;

            // Calculate required buffer size (including null terminator)
            var bufferSize = textLength + 1;

            // Allocate buffer
            var buffer = bufferSize <= 256
                ? stackalloc char[bufferSize]
                : new char[bufferSize];

            unsafe
            {
                fixed (char* bufferPtr = buffer)
                {
                    // Send WM_GETTEXT message
                    var result = PInvoke.SendMessage(
                        hWnd,
                        PInvoke.WM_GETTEXT,
                        (nuint)bufferSize,  // Correct conversion to WPARAM
                        (nint)bufferPtr      // Correct conversion to LPARAM
                    );

                    var charsCopied = (int)result.Value;
                    return charsCopied > 0
                        ? new string(bufferPtr, 0, charsCopied)
                        : string.Empty;
                }
            }
        }
        catch
        {
            // ignored
        }

        return string.Empty;
    }

    /// <summary>
    /// Creates a task which starts the external application's process.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="InvalidOperationException">Thrown when the application has already been started.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (ApplicationState != ApplicationState.Stopped)
            throw new InvalidOperationException("Cannot start application because it is already starting or running.");

        try
        {
            ApplicationState = ApplicationState.Starting;

            Process? process = null;
            if (!Configuration.UseExistingProcess)
            {
                process = await StartProcessAsync(Configuration, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(Configuration.ProcessNameToTrack) ||
                !string.IsNullOrWhiteSpace(Configuration.CommandLineMatchString))
            {
                process = await FindProcessAsync(
                    Configuration.ProcessNameToTrack,
                    Configuration.CommandLineMatchString,
                    Configuration.MinWaitTime,
                    Configuration.MaxWaitTime,
                    cancellationToken);
            }

            if (!string.IsNullOrEmpty(Configuration.WindowTitleMatch))
            {
                await Task.Delay(Configuration.MinWaitTime * 1000, cancellationToken);
                var window = await FindWindowHandleAsync(
                    process,
                    Configuration.WindowTitleMatch,
                    Configuration.WindowTitleMatchSkip,
                    Configuration.MaxWaitTime,
                    cancellationToken);

                if (window == null)
                {
                    var queryWindowEventArgs = new QueryWindowEventArgs();
                    QueryWindow?.Invoke(this, queryWindowEventArgs);
                    if (queryWindowEventArgs.WindowHandle == 0)
                    {
                        throw new MissingWindowException();
                    }
                    var provider = new ProcessWindowProvider(_loggerFactory.CreateLogger<ProcessWindowProvider>());
                    window = provider
                        .GetProcessWindows()
                        .First(w => w.WindowHandle == (IntPtr)queryWindowEventArgs.WindowHandle);
                }

                if (window == null)
                {
                    ApplicationState = ApplicationState.Stopped;
                    throw new InvalidOperationException("No valid window was specified.");
                }

                process = Process.GetProcessById(window.ProcessId);

            }

            if (process == null)
            {
                ApplicationState = ApplicationState.Stopped;
                throw new InvalidOperationException("Failed to start or capture process.");
            }

            Process = process;
            Process.EnableRaisingEvents = true;
            Process.Exited += AppProcess_Exited;
            WindowHandle = new HWND(process.MainWindowHandle);
            ApplicationState = ApplicationState.Running;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{Method} has been cancelled", nameof(StartAsync));
            ApplicationState = ApplicationState.Stopped;
        }
    }

    private static async Task<Process?> StartProcessAsync(ExternalAppConfiguration configuration, CancellationToken cancellationToken)
    {
        // Prepare Process for Execution and Start
        var process = StartProcess(configuration);
        if (process == null)
            throw new InvalidOperationException($"Failed to start process \"{configuration.Executable}\"");

        try
        {
            process.WaitForInputIdle();
        }
        catch
        {
            // ignore: PowerShell doesn't allow to wait for input idle
        }

        if (process.HasExited)
        {
            throw new InvalidOperationException(
                "The process is not running anymore or does not have a main window handle.");
        }


        // Wait for the App to be started, try to get the main window handle
        // depending on the configuration, we don't always need the main window handle of the process we started
        Exception? lastException = null;

        var tryCount = 0;
        var mainWindowHandle = IntPtr.Zero;
        var mainWindowTitle = "Default IME";
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (tryCount >= configuration.MaxWaitTime * 4)
                break;

            try
            {
                process.Refresh();

                await Task.Delay(100, cancellationToken);

                lastException = null;
                mainWindowHandle = process.MainWindowHandle;
                mainWindowTitle = process.MainWindowTitle;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            tryCount++;
        } while (!process.HasExited && (mainWindowHandle == IntPtr.Zero || mainWindowTitle == "Default IME"));

        if (process.HasExited)
        {
            throw new InvalidOperationException(
                "The process is not running anymore or does not have a main window handle.");
        }

        if (lastException != null)
        {
            throw new InvalidOperationException(
                $"Failed to start process after {tryCount} retries. See inner exception.", lastException);
        }

        return process;
    }

    private static Process? StartProcess(ExternalAppConfiguration configuration)
    {
        var processStartInfo = new ProcessStartInfo(
            configuration.Executable ?? string.Empty,
            configuration.Arguments ?? string.Empty)
        {
            WindowStyle = configuration is {StartHidden: true, StartExternal: false}
                ? ProcessWindowStyle.Minimized
                : ProcessWindowStyle.Normal,
            CreateNoWindow = configuration is {StartHidden: true, StartExternal: false},
            UseShellExecute = true,
            WorkingDirectory = configuration.WorkingDirectory ?? ".",
            LoadUserProfile = configuration.LoadUserProfile,
        };

        if (configuration.RunElevated)
        {
            processStartInfo.UseShellExecute = true;
            processStartInfo.Verb = "runas";
        }
        else if (configuration.UseCredentials)
        {
            processStartInfo.UseShellExecute = false;
            processStartInfo.Verb = "runas";
            if (configuration.Username != null)
                processStartInfo.UserName = configuration.Username;
            if (configuration.Domain != null)
                processStartInfo.Domain = configuration.Domain;
            if (configuration.Password != null)
                processStartInfo.Password = SecureStringExtensions.ConvertToSecureString(configuration.Password);
        }

        var process = Process.Start(processStartInfo);

        return process;
    }

    private async Task<Process?> FindProcessAsync(string? processNameToTrack, string? commandLineMatch, int minWaitTime, int maxWaitTime, CancellationToken cancellationToken)
    {
        const string win32Processes = "SELECT ProcessId, CommandLine FROM Win32_Process";

        // setup WMI
        var wmiQuery = string.IsNullOrWhiteSpace(processNameToTrack)
            ? $"{win32Processes} WHERE Name='{processNameToTrack}'"
            : win32Processes;

        var searcher = new ManagementObjectSearcher(wmiQuery);
        _logger.LogDebug("WMI query to execute: {WmiQuery}", wmiQuery);

        var debugInfo = new StringBuilder();
        debugInfo.AppendLine($"Process name to track: {processNameToTrack}");
        debugInfo.AppendLine($"Command line fragment to look for: {commandLineMatch}");

        // assign local variables to prevent leaking memory by capturing members in async methods
        var minWaitTimeInMs = minWaitTime * 1000;

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

                var commandLine = wmiWin32Process["CommandLine"].ToString();
                var processIdString = wmiWin32Process["ProcessId"].ToString();

                // check if we can parse the process id and if we have a valid command line
                if (!int.TryParse(processIdString, out var processId))
                    continue;

                if (string.IsNullOrWhiteSpace(commandLine) || processId <= -1)
                    continue;

                debugInfo.AppendLine($"Process: {processIdString}, Command Line: {commandLine}");

                // if no command line match string has been provided, take the process we found
                // or in case a command line match string has been provided, check if it matches
                if (!string.IsNullOrWhiteSpace(commandLineMatch) &&
                    !commandLine.ToLower().Contains(commandLineMatch!.ToLower()))
                    continue;

                _logger.LogDebug("{Method} succeeded - Details: {Details}", nameof(FindProcessAsync), debugInfo);

                return Process.GetProcessById(processId);
            }

            tryCount++;
            await Task.Delay(250, cancellationToken);

            if (tryCount >= maxWaitTime * 4)
                break;
        }

        _logger.LogDebug("{Method} failed - Details: {Details}", nameof(FindProcessAsync), debugInfo);

        return null;
    }

    private async Task<ProcessWindowInfo?> FindWindowHandleAsync(Process? process, string titleMatch,
        int titleMatchSkip, int maxWaitTime, CancellationToken cancellationToken)
    {
        var tryCountMatch = 0;
        var titleMatches = 0;

        var currentWindowHandle = process != null ? new HWND(process.MainWindowHandle) : HWND.Null;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var provider = new ProcessWindowProvider(_loggerFactory.CreateLogger<ProcessWindowProvider>());
            foreach (var window in provider.GetProcessWindows())
            {
                if (process != null && window.ProcessId != process.Id)
                    continue;

                // title matches
                if (!window.WindowTitle.Contains(titleMatch))
                    continue;

                // we are still on the same window
                if (titleMatchSkip > 0 && currentWindowHandle == window.WindowHandle)
                    continue;

                // if window title matches should be skipped
                if (titleMatches <= titleMatchSkip)
                {
                    titleMatches++;
                    break;
                }

                // remember the current window handle
                return window;
            }

            await Task.Delay(250, cancellationToken);
            tryCountMatch++;
        } while (tryCountMatch < maxWaitTime * 4);

        return null;
    }

    private void AppProcess_Exited(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Process process)
                process.Exited -= AppProcess_Exited;

            ProcessExited?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ApplicationClosed event threw an exception");
        }
    }

    public async Task EmbedAsync(ExternalAppHost ownerControl, CancellationToken cancellationToken)
    {
        switch (Configuration.EmbedMethod)
        {
            case EmbedMethod.Control:
                await SetParentAsync(ownerControl.ControlHandle, WindowHandle, cancellationToken);
                IsEmbedded = true;
                ownerControl.SetWindowPosition();
                break;
            case EmbedMethod.Window:
                OriginalWindowHandle = WindowHandle;
                ownerControl.Invoke(() =>
                {
                    WindowHandle = ExternalApps.EmbedWindow(ownerControl.ControlHandle, WindowHandle, Process, _logger);
                });
                IsEmbedded = true;
                break;
        }
    }

    /// <summary>
    /// Detaches the external application window.
    /// </summary>
    public void DetachApplication()
    {
        if (!HasWindow)
            return;

        switch (Configuration.EmbedMethod)
        {
            case EmbedMethod.Control:
                PInvoke.SetParent(WindowHandle, new HWND(IntPtr.Zero));
                // ensure WS_DISABLED is always removed
                _originalGwlStyle = _originalGwlStyle & ~WINDOW_STYLE.WS_DISABLED;
                PInvoke.SetWindowLong(WindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)_originalGwlStyle);
                IsEmbedded = false;
                break;
            case EmbedMethod.Window:
                WindowHandle = ExternalApps.DetachWindow(WindowHandle);
                IsEmbedded = false;
                break;
        }
    }

    /// <summary>
    /// Focus the external application window
    /// </summary>
    public void FocusApplication()
    {
        switch (Configuration.EmbedMethod)
        {
            case EmbedMethod.Control:
                PInvoke.SetForegroundWindow(WindowHandle);
                PInvoke.SetFocus(WindowHandle);
                break;
            case EmbedMethod.Window:
                PInvoke.BringWindowToTop(OriginalWindowHandle);
                break;
        }
    }

    /// <summary>
    /// Sets the external application's window position.
    /// </summary>
    /// <param name="x">The window's left value.</param>
    /// <param name="y">The window's top value.</param>
    /// <param name="width">The window's width.</param>
    /// <param name="height">The window's height.</param>
    public void SetWindowPosition(int x, int y, int width, int height)
    {
        if (!HasWindow)
            return;

        // the coordinates of the client area rectangle
        var rect = new RECT
        {
            left = x,
            top = y,
            right = x + width,
            bottom = y + height,
        };

        // let windows calculate the best position for the window when we want to have the client rect at those coordinates
        PInvoke.AdjustWindowRectEx(ref rect, _embeddedGwlStyle, false, WINDOW_EX_STYLE.WS_EX_LEFT);

        // let's move the window
        PInvoke.MoveWindow(
            WindowHandle,
            rect.left,
            rect.top,
            rect.right - rect.left,
            rect.bottom - rect.top,
            true);
    }

    private async Task SetParentAsync(HWND parentWindowHandle, HWND childWindowHandle, CancellationToken cancellationToken)
    {
        var retry = 0;
        bool success;

        // remember the original window style (currently not in use because application of old style doesn't always work)
        _originalGwlStyle = (WINDOW_STYLE) PInvoke.GetWindowLong(
            childWindowHandle,
            WINDOW_LONG_PTR_INDEX.GWL_STYLE);

        // setting these styles don't work because keyboard input is broken afterwards
        var newStyle = _originalGwlStyle & ~(WINDOW_STYLE.WS_GROUP | WINDOW_STYLE.WS_TABSTOP) | WINDOW_STYLE.WS_CHILD;

        PInvoke.SetWindowLong(childWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int) newStyle);

        do
        {
            try
            {
                PInvoke.SetParent(childWindowHandle, parentWindowHandle);
                var lastWin32Exception = new Win32Exception();
                success = lastWin32Exception.NativeErrorCode == 0;

                _logger.LogDebug(success ? null : lastWin32Exception,
                    "SetParentAsync success: {Success}, Error Code: {NativeErrorCode}",
                    success, lastWin32Exception.NativeErrorCode);
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogDebug(ex, "SetParentInternal failed");
            }

            if (success || retry > 10)
                break;

            retry++;

            await Task.Delay(250, cancellationToken);
        } while (true);

        if (success)
        {
            _embeddedGwlStyle = (WINDOW_STYLE) PInvoke.GetWindowLong(
                childWindowHandle,
                WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        }
    }
}
