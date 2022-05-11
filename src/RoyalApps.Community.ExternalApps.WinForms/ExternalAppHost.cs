// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable StringLiteralTypo

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

using Microsoft.Extensions.Logging;

using RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Microsoft.Extensions.Logging.Abstractions;

namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// The host control which can embed external application windows.
/// </summary>
public class ExternalAppHost : UserControl
{
    private HWND _ownerHandle;

    private ILogger? _logger;
    private ILogger Logger
    {
        get
        {
            _logger ??= LoggerFactory.CreateLogger<ExternalAppHost>();
            return _logger;
        }
    }

    /// <summary>
    /// A <see cref="ILoggerFactory"/> used to create instances of <see cref="ILogger"/>. Defaults to <see cref="NullLoggerFactory"/>.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    private ExternalApp? _externalApp;

    private Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE _originalGwlStyle;

    private Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE _embeddedGwlStyle;

    // ReSharper disable once CollectionNeverQueried.Local

    private readonly List<WINEVENTPROC> _winEventProcs = new();

    private readonly List<IntPtr> _winEventHooks = new();

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationActivated;

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationStarted;

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationClosed;

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<EventArgs>? WindowTitleChanged;
    
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_externalApp != null)
                _externalApp.ProcessExited -= ExternalApp_ProcessExited;
            
            _winEventHooks.ForEach(delegate (IntPtr hook) { PInvoke.UnhookWinEvent(new HWINEVENTHOOK(hook)); });
            _winEventHooks.Clear();
            _winEventProcs.Clear();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ownerHandle = new HWND(Handle);
    }

    /// <summary>
    /// 
    /// </summary>
    public void CloseApplication()
    {
        _externalApp?.CloseApplication();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    public void Start(ExternalAppConfiguration configuration)
    {
        var taskFactory = new TaskFactory();
        taskFactory.StartNew(() => StartAsync(configuration));
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
        else
        {
            // TODO: RaiseApplicationClosed
        }
    }

    private void ExternalApp_ProcessExited(object? sender, EventArgs e)
    {
        RaiseApplicationClosed();
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

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public NativeResult EmbedApplication()
    {
        if (_externalApp is {HasWindow: false})
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
    /// 
    /// </summary>
    /// <param name="externalWindowActivation"></param>
    public void FocusApplication(bool externalWindowActivation)
    {
        OnFocusApplication(externalWindowActivation);
    }

    /// <summary>
    /// 
    /// </summary>
    public void FreeApplication()
    {
        if (_externalApp is null or {HasWindow: false})
            return;

        var handle = _externalApp.WindowHandle;
        
        PInvoke.SetParent(handle, new HWND(IntPtr.Zero));
        PInvoke.SetWindowLong(handle, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)_originalGwlStyle);
        
        _externalApp.IsEmbedded = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Bitmap? GetWindowScreenshot()
    {
        if (_externalApp is null or {HasWindow: false})
            return null;

        PInvoke.GetWindowRect(_externalApp.WindowHandle, out var rect);

        if (rect.right == 0 || rect.bottom == 0)
            return null;

        var bmp = new Bitmap(rect.right, rect.bottom, PixelFormat.Format32bppArgb);
        var gfxBmp = Graphics.FromImage(bmp);
        var hdcBitmap = gfxBmp.GetHdc();
        var hdc = new Windows.Win32.Graphics.Gdi.HDC(hdcBitmap);

        PInvoke.PrintWindow(_externalApp.WindowHandle, hdc, Windows.Win32.Storage.Xps.PRINT_WINDOW_FLAGS.PW_CLIENTONLY);

        gfxBmp.ReleaseHdc(hdcBitmap);
        gfxBmp.Dispose();

        return bmp;
    }

    /// <summary>
    /// 
    /// </summary>
    public void MaximizeApplication()
    {
        if (_externalApp is not {HasWindow: true}) 
            return;
        
        PInvoke.ShowWindow(_externalApp.WindowHandle, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED);
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetWindowPosition()
    {
        if (Disposing || IsDisposed)
            return;

        try
        {
            if (_externalApp is {IsEmbedded: true})
            {
                SetWindowPosition(0, 0, Width, Height);
            }
            else
            {
                if (_externalApp != null)
                    PInvoke.ShowWindow(_externalApp.WindowHandle,
                        Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWDEFAULT);
                
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
    /// 
    /// </summary>
    /// <param name="rectangle"></param>
    public void SetWindowPosition(Rectangle rectangle)
    {
        SetWindowPosition(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void SetWindowPosition(int x, int y, int width, int height)
    {
        if (_externalApp is null or {HasWindow: false})
            return;

        // the coordinates of the client area rectangle
        var rect = new RECT
        {
            left = x,
            top = y,
            right = x + width,
            bottom = y + height
        };


        // let windows calculate the best position for the window when we want to have the client rect at those coordinates
        PInvoke.AdjustWindowRectEx(ref rect, _embeddedGwlStyle, false, Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LEFT);

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
    /// 
    /// </summary>
    protected virtual void OnApplicationActivated()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnApplicationClosed()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnApplicationStarted()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnWindowTitleChanged()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="externalWindowActivation"></param>
    protected virtual void OnFocusApplication(bool externalWindowActivation)
    {
        if (_externalApp is null or { HasWindow: false})
            return;

        if (!_externalApp.IsEmbedded && !externalWindowActivation) 
            return;
        
        Focus();
        PInvoke.SetForegroundWindow(_externalApp.WindowHandle);
        PInvoke.SetFocus(_externalApp.WindowHandle);
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

    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        SetWindowPosition();
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
                // notify host application that the external app area has been clicked
                RaiseApplicationActivated();
                // make sure the external application gets the input focus
                FocusApplication(false);
                break;
        }
        base.WndProc(ref m);
    }

    private NativeResult SetParent(HWND parentHandle)
    {
        var result = NativeResult.Fail();
        var retry = 0;

        // remember the original window style (currently not in use because application of old style doesn't always work)
        if (_externalApp != null)
        {
            _originalGwlStyle = (Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE) PInvoke.GetWindowLong(
                _externalApp.WindowHandle, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            // setting these styles don't work because keyboard input is broken afterwards
            var newStyle =
                _originalGwlStyle &
                ~(Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_GROUP |
                  Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_TABSTOP)
                | Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_CHILD;
            PInvoke.SetWindowLong(_externalApp.WindowHandle,
                Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int) newStyle);

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
            } while (true);

            if (result.Succeeded)
            {
                _embeddedGwlStyle = (Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE) PInvoke.GetWindowLong(
                    _externalApp.WindowHandle, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE);
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
        if (_externalApp is null or {IsRunning: false})
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