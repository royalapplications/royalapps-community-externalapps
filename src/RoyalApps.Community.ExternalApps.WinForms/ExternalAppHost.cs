namespace RoyalApps.Community.ExternalApps.WinForms;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WindowManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.Xps;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

/// <summary>
/// The host control which can embed external application windows.
/// </summary>
public class ExternalAppHost : UserControl
{
    private readonly List<IntPtr> _winEventHooks = new();

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<WINEVENTPROC> _winEventProcs = new();

    private WINDOW_STYLE _embeddedGwlStyle;
    private WINDOW_STYLE _originalGwlStyle;
    private HWND _ownerHandle;

    private ExternalApp? _externalApp;
    private ILogger? _logger;

    /// <summary>
    /// Fired after the application has been activated.
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationActivated;

    /// <summary>
    /// Fired after the application has been closed.
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationClosed;

    /// <summary>
    /// Fired after the application has been started.
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationStarted;

    /// <summary>
    /// Fired when the application's window title has changed.
    /// </summary>
    public event EventHandler<EventArgs>? WindowTitleChanged;

    /// <summary>
    /// Gets or sets the <see cref="ILoggerFactory" /> used to create instances of <see cref="ILogger" />.
    /// Defaults to <see cref="NullLoggerFactory" />.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    /// <summary>
    /// Gets the logger from the configured <see cref="LoggerFactory"/>.
    /// </summary>
    protected ILogger Logger
    {
        get
        {
            _logger ??= LoggerFactory.CreateLogger<ExternalAppHost>();
            return _logger;
        }
    }

    /// <summary>
    /// Closes the external application
    /// </summary>
    public void CloseApplication()
    {
        _externalApp?.CloseApplication();
    }

    /// <summary>
    /// Embeds the application.
    /// </summary>
    /// <returns>A <see cref="NativeResult"/> indicating whether the operation was successful.</returns>
    public NativeResult EmbedApplication()
    {
        if (_externalApp is { HasWindow: false })
        {
            // process not found or application has been closed
            RaiseApplicationClosed();
            return NativeResult.Fail();
        }

        var result = SetParent(_ownerHandle);
        if (!result.Succeeded)
            return result;

        SetWindowPosition();
        FocusApplication(false);
        return NativeResult.Success;
    }

    /// <summary>
    /// Starts a new external application.
    /// </summary>
    /// <param name="configuration">The <see cref="ExternalAppConfiguration"/> to apply.</param>
    public void Start(ExternalAppConfiguration configuration)
    {
        var taskFactory = new TaskFactory();
        taskFactory.StartNew(() => StartAsync(configuration));
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ownerHandle = new HWND(Handle);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_externalApp != null)
                _externalApp.ProcessExited -= ExternalApp_ProcessExited;

            _winEventHooks.ForEach(delegate(IntPtr hook) { PInvoke.UnhookWinEvent(new HWINEVENTHOOK(hook)); });
            _winEventHooks.Clear();
            _winEventProcs.Clear();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Focuses the external application. 
    /// </summary>
    /// <param name="externalWindowActivation"></param>
    public void FocusApplication(bool externalWindowActivation)
    {
        OnFocusApplication(externalWindowActivation);
    }

    /// <summary>
    /// Frees the external application window.
    /// </summary>
    public void FreeApplication()
    {
        if (_externalApp is null or { HasWindow: false })
            return;

        var handle = _externalApp.WindowHandle;

        PInvoke.SetParent(handle, new HWND(IntPtr.Zero));
        PInvoke.SetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)_originalGwlStyle);

        _externalApp.IsEmbedded = false;
    }

    /// <summary>
    /// Creates a screenshot of the external application's window.
    /// </summary>
    /// <returns>The screenshot <see cref="Bitmap"/>.</returns>
    public Bitmap? GetWindowScreenshot()
    {
        if (_externalApp is null or { HasWindow: false })
            return null;

        PInvoke.GetWindowRect(_externalApp.WindowHandle, out var rect);

        if (rect.right == 0 || rect.bottom == 0)
            return null;

        var bmp = new Bitmap(rect.right, rect.bottom, PixelFormat.Format32bppArgb);
        var gfxBmp = Graphics.FromImage(bmp);
        var hdcBitmap = gfxBmp.GetHdc();
        var hdc = new HDC(hdcBitmap);

        PInvoke.PrintWindow(_externalApp.WindowHandle, hdc, PRINT_WINDOW_FLAGS.PW_CLIENTONLY);

        gfxBmp.ReleaseHdc(hdcBitmap);
        gfxBmp.Dispose();

        return bmp;
    }

    /// <summary>
    /// Maximizes the external application.
    /// </summary>
    public void MaximizeApplication()
    {
        if (_externalApp is not { HasWindow: true })
            return;

        PInvoke.ShowWindow(_externalApp.WindowHandle, SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED);
    }

    /// <summary>
    /// Sets the external application's window position to the default values.
    /// </summary>
    public void SetWindowPosition()
    {
        if (Disposing || IsDisposed)
            return;

        try
        {
            if (_externalApp is { IsEmbedded: true })
            {
                SetWindowPosition(0, 0, Width, Height);
            }
            else
            {
                if (_externalApp != null)
                    PInvoke.ShowWindow(_externalApp.WindowHandle, SHOW_WINDOW_CMD.SW_SHOWDEFAULT);

                SetWindowPosition(new Rectangle(
                    PointToScreen(new Point(Left - SystemInformation.Border3DSize.Width, Top)).X,
                    PointToScreen(new Point(Left - SystemInformation.Border3DSize.Width, Top)).Y,
                    Width,
                    Height));
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Cannot set the window position");
        }
    }

    /// <summary>
    /// Sets the external application's window position.
    /// </summary>
    /// <param name="rectangle">A <see cref="Rectangle"/> describing the desired position.</param>
    public void SetWindowPosition(Rectangle rectangle)
    {
        SetWindowPosition(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
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
        if (_externalApp is null or { HasWindow: false })
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
            _externalApp.WindowHandle,
            rect.left,
            rect.top,
            rect.right - rect.left,
            rect.bottom - rect.top,
            true);
    }

    /// <summary>
    /// </summary>
    protected virtual void OnApplicationActivated()
    {
    }

    /// <summary>
    /// </summary>
    protected virtual void OnApplicationClosed()
    {
    }

    /// <summary>
    /// </summary>
    protected virtual void OnApplicationStarted()
    {
    }

    /// <summary>
    /// Handles focusing of the external application. 
    /// </summary>
    /// <param name="externalWindowActivation"></param>
    protected virtual void OnFocusApplication(bool externalWindowActivation)
    {
        if (_externalApp is null or { HasWindow: false })
            return;

        if (!_externalApp.IsEmbedded && !externalWindowActivation)
            return;

        Focus();

        PInvoke.SetForegroundWindow(_externalApp.WindowHandle);
        PInvoke.SetFocus(_externalApp.WindowHandle);
    }

    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        SetWindowPosition();
    }

    /// <summary>
    /// Handles a window title change.
    /// </summary>
    protected virtual void OnWindowTitleChanged()
    {
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        switch ((uint)m.Msg)
        {
            case PInvoke.WM_MOUSEACTIVATE:
            case PInvoke.WM_LBUTTONDOWN:
            case PInvoke.WM_MDIACTIVATE:
            case PInvoke.WM_SETFOCUS:
            {
                // notify host application that the external app area has been clicked
                RaiseApplicationActivated();

                // make sure the external application gets the input focus
                FocusApplication(false);
                break;
            }
        }

        base.WndProc(ref m);
    }

    private void ExternalApp_ProcessExited(object? sender, EventArgs e)
    {
        RaiseApplicationClosed();
    }

    private void RaiseApplicationActivated()
    {
        LogVerboseInDebugOnly(nameof(RaiseApplicationActivated));
        OnApplicationActivated();
        ApplicationActivated?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseApplicationClosed()
    {
        if (InvokeRequired)
        {
            Invoke(RaiseApplicationClosed);
            return;
        }

        LogVerboseInDebugOnly(nameof(RaiseApplicationClosed));
        OnApplicationClosed();
        ApplicationClosed?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseApplicationStarted()
    {
        LogVerboseInDebugOnly(nameof(RaiseApplicationStarted));
        OnApplicationStarted();
        ApplicationStarted?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseWindowTitleChanged()
    {
        LogVerboseInDebugOnly(nameof(RaiseWindowTitleChanged));
        OnWindowTitleChanged();
        WindowTitleChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task StartAsync(ExternalAppConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _externalApp = new ExternalApp(configuration, LoggerFactory);
        _externalApp.ProcessExited += ExternalApp_ProcessExited;

        var result = await _externalApp.StartAsync(cancellationToken);

        if (result.Succeeded)
        {
            Invoke(StartedSuccessful);
        }
    }

    private void StartedSuccessful()
    {
        var result = NativeResult.Fail();
        if (_externalApp != null && !_externalApp.Configuration.StartExternal)
        {
            try
            {
                result = EmbedApplication();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Embedding application failed");
            }
        }

        if (!result.Succeeded)
            Logger.LogWarning(result.Exception, "StartApplicationInternalAsync raised an error");

        SetupHooks();
        SetWindowPosition();
        RaiseApplicationStarted();
    }

    private NativeResult SetParent(HWND parentHandle)
    {
        var result = NativeResult.Fail();
        var retry = 0;

        // remember the original window style (currently not in use because application of old style doesn't always work)
        if (_externalApp != null)
        {
            _originalGwlStyle = (WINDOW_STYLE)PInvoke.GetWindowLong(_externalApp.WindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);

            // setting these styles don't work because keyboard input is broken afterwards
            var newStyle =
                _originalGwlStyle &
                ~(WINDOW_STYLE.WS_GROUP |
                  WINDOW_STYLE.WS_TABSTOP)
                | WINDOW_STYLE.WS_CHILD;
            PInvoke.SetWindowLong(_externalApp.WindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)newStyle);

            // this needs to run asynchronously to not block the UI thread
            do
            {
                try
                {
                    result = SetParentInternal(parentHandle);
                }
                catch (Exception ex)
                {
                    result = NativeResult.Fail(ex);
                    Logger.LogDebug(ex, "SetParentInternal failed");
                }

                if (result.Succeeded || retry > 10)
                    break;

                retry++;
                Thread.Sleep(100);
            }
            while (true);

            if (result.Succeeded)
            {
                _embeddedGwlStyle = (WINDOW_STYLE)PInvoke.GetWindowLong(
                    _externalApp.WindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            }

            _externalApp.IsEmbedded = result.Succeeded;
        }

        return result;
    }

    private NativeResult SetParentInternal(HWND parentHandle)
    {
        // BeginInvoke is needed here!
        // When not executed at the end of the message pump, the SetParent call always returns '5' (Access Denied)
        var asyncResult = BeginInvoke(new MethodInvoker(() =>
        {
            // https://devblogs.microsoft.com/oldnewthing/?p=4683
            if (_externalApp != null)
                PInvoke.SetParent(_externalApp.WindowHandle, parentHandle);
        }));

        // we need to wait for the async code to finish and get the last win32 error code
        EndInvoke(asyncResult);

        var lastWin32Exception = new Win32Exception();
        var success = lastWin32Exception.NativeErrorCode == 0;

        Logger.LogDebug(lastWin32Exception, "SetParentInternal success: {Success}, Error Code: {NativeErrorCode}", success, lastWin32Exception.NativeErrorCode);

        return success
            ? NativeResult.Success
            : NativeResult.Fail(lastWin32Exception);
    }

    private void SetupHooks()
    {
        if (_externalApp is null or { IsRunning: false })
            return;

        var winEventProc = new WINEVENTPROC(WinEventProc);

        // always add the delegate to the _winEventProcs list
        // otherwise GC will dump this delegate and the hook fails
        _winEventProcs.Add(winEventProc);
        var eventType = PInvoke.EVENT_OBJECT_NAMECHANGE;

        var hook = PInvoke.SetWinEventHook(
            eventType,
            eventType,
            new HINSTANCE(IntPtr.Zero),
            WinEventProc,
            (uint)_externalApp.Process!.Id,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT);

        _winEventHooks.Add(hook);
    }

    // ReSharper disable once IdentifierTypo
    private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        LogVerboseInDebugOnly($"WinEventProc: EventType: {eventType}, Window Handle: {hWnd}, idObject: {idObject}, idChild: {idChild}");

        if (_externalApp == null)
            return;

        if (_externalApp.WindowHandle != hWnd)
        {
            LogVerboseInDebugOnly(
                $"WinEventProc: exiting because WindowHandle ({_externalApp.WindowHandle}) != hWnd ({hWnd})");
            return;
        }

        switch (eventType)
        {
            case PInvoke.EVENT_OBJECT_NAMECHANGE:
                LogVerboseInDebugOnly($"WinEventProc: EVENT_OBJECT_NAMECHANGE: {_externalApp.GetWindowTitle()}");
                Invoke(RaiseWindowTitleChanged);
                break;
        }
    }

    private void LogVerboseInDebugOnly(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        #if DEBUG
        Debug.WriteLine(message);
        #endif
    }
}