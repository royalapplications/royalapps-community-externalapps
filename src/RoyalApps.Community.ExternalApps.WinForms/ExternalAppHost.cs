using RoyalApps.Community.ExternalApps.WinForms.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RoyalApps.Community.ExternalApps.WinForms.WindowManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.Xps;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;
using System.ComponentModel;
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// The host control which can embed external application windows.
/// </summary>
public class ExternalAppHost : Control
{
    private readonly List<IntPtr> _winEventHooks = new();

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<WINEVENTPROC> _winEventProcedures = new();

    private bool IsLeftMouseButtonDown => MouseButtons.HasFlag(MouseButtons.Left);

    /// <summary>
    /// The Handle property as HWND.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal HWND ControlHandle { get; private set; }

    private ExternalApp? _externalApp;
    private ILogger? _logger;

    /// <summary>
    /// Gets or sets the <see cref="ILoggerFactory" /> used to create instances of <see cref="ILogger" />.
    /// Defaults to <see cref="NullLoggerFactory" />.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ILoggerFactory LoggerFactory { get; set; }

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
    /// Gets the original window handle of the embedded window.
    /// </summary>
    public IntPtr EmbeddedWindowHandle
    {
        get
        {
            if (_externalApp != null)
                return _externalApp.Configuration.EmbedMethod == EmbedMethod.Window
                    ? _externalApp.OriginalWindowHandle
                    : _externalApp.WindowHandle;
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// The configuration of the external application to embed.
    /// </summary>
    public ExternalAppConfiguration? Configuration => _externalApp?.Configuration;

    /// <summary>
    /// True if an external application window is currently embedded.
    /// </summary>
    public bool IsEmbedded => _externalApp?.IsEmbedded ?? false;

    /// <summary>
    /// Provides access to the actual process object of the embedded window.
    /// </summary>
    public Process? Process => _externalApp?.Process;

    /// <summary>
    /// Raised after the application has been activated.
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationActivated;

    /// <summary>
    /// Raised after the application has been closed.
    /// </summary>
    public event EventHandler<ApplicationClosedEventArgs>? ApplicationClosed;

    /// <summary>
    /// Raised after the application has been started.
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationStarted;

    /// <summary>
    /// Raised when no window with the matching criteria has been found.
    /// </summary>
    public event EventHandler<QueryWindowEventArgs>? QueryWindow;

    /// <summary>
    /// Raised when the application's window title has changed.
    /// </summary>
    public event EventHandler<EventArgs>? WindowTitleChanged;

    /// <summary>
    /// Constructor
    /// </summary>
    public ExternalAppHost(ILoggerFactory? loggerFactory = null)
    {
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        SetStyle(ControlStyles.ContainerControl, false);
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    /// <summary>
    /// Closes the external application
    /// </summary>
    public void CloseApplication()
    {
        _externalApp?.CloseApplication();
    }

    /// <summary>
    /// Detaches the application.
    /// </summary>
    public void DetachApplication()
    {
        _externalApp?.DetachApplication();
        SetWindowPosition();
    }

    /// <summary>
    /// Embeds the application.
    /// </summary>
    public void EmbedApplication()
    {
        if (_externalApp is null)
            return;

        var taskFactory = new TaskFactory();
        taskFactory.StartNew(() => _externalApp.EmbedAsync(this, CancellationToken.None), TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Embeds the application.
    /// </summary>
    private async Task EmbedApplicationAsync(CancellationToken cancellationToken = default)
    {
        if (_externalApp is null or {HasWindow: false})
        {
            // process not found or application has been closed
            throw new MissingWindowException();
        }

        await _externalApp.EmbedAsync(this, cancellationToken);

        Invoke(() =>
        {
            SetWindowPosition();
            FocusApplication(false);
        });
    }

    /// <summary>
    /// Starts a new external application.
    /// </summary>
    /// <param name="configuration">The <see cref="ExternalAppConfiguration"/> to apply.</param>
    public void Start(ExternalAppConfiguration configuration)
    {
        var taskFactory = new TaskFactory();
        taskFactory.StartNew(() => StartAsync(configuration), TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Shows the system menu of the embedded app.
    /// </summary>
    /// <param name="location">The location the menu should appear.</param>
    public void ShowSystemMenu(Point location)
    {
        if (_externalApp == null)
            return;

        ExternalApps.ShowSystemMenu(new HWND(EmbeddedWindowHandle),
            ControlHandle,
            location);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_externalApp != null)
            {
                _externalApp.ProcessExited -= ExternalApp_ProcessExited;
                _externalApp.QueryWindow -= ExternalApp_QueryWindow;
                _externalApp.Dispose();
                _externalApp = null;
            }

            _winEventHooks.ForEach(delegate(IntPtr hook) { PInvoke.UnhookWinEvent(new HWINEVENTHOOK(hook)); });
            _winEventHooks.Clear();
            _winEventProcedures.Clear();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ControlHandle = new(Handle);
    }

    /// <summary>
    /// Focuses the external application.
    /// </summary>
    /// <param name="force">If true, always bring external application to foreground and focus the window, even if it is not embedded.</param>
    public void FocusApplication(bool force)
    {
        OnFocusApplication(force);
    }

    /// <summary>
    /// Creates a screenshot of the external application's window.
    /// </summary>
    /// <returns>The screenshot <see cref="Bitmap"/>.</returns>
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
        if (_externalApp is not {HasWindow: true})
            return;

        PInvoke.ShowWindow(_externalApp.WindowHandle, SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED);
    }

    /// <summary>
    /// Sets the external application's window position.
    /// </summary>
    /// <param name="rectangle">A <see cref="Rectangle"/> describing the desired position.</param>
    public void SetWindowPosition(Rectangle rectangle)
    {
        _externalApp?.SetWindowPosition(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
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
    /// </summary>
    protected virtual void OnQueryWindow(QueryWindowEventArgs e)
    {
    }

    /// <summary>
    /// Handles a window title change.
    /// </summary>
    protected virtual void OnWindowTitleChanged()
    {
    }

    /// <summary>
    /// Handles focusing of the external application.
    /// </summary>
    /// <param name="force">If true, always bring external application to foreground and focus the window, even if it is not embedded.</param>
    protected virtual void OnFocusApplication(bool force)
    {
        if (_externalApp is null or {HasWindow: false})
            return;

        if (!_externalApp.IsEmbedded && !force)
            return;

        Invoke(Focus);

        _externalApp.FocusApplication();
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
        switch ((uint) m.Msg)
        {
            case PInvoke.WM_APP when (uint)m.WParam == PInvoke.WM_SETFOCUS:
                RaiseApplicationActivated();
                break;
            case PInvoke.WM_SETFOCUS:
                FocusApplication(false);
                break;
            case PInvoke.WM_MOUSEACTIVATE or PInvoke.WM_LBUTTONDOWN or PInvoke.WM_MDIACTIVATE when !IsLeftMouseButtonDown:
            {
                FocusApplication(false);
                break;
            }
        }

        base.WndProc(ref m);
    }

    private void ExternalApp_ProcessExited(object? sender, EventArgs e)
    {
        RaiseApplicationClosed(new ApplicationClosedEventArgs {ProcessExited = true});
    }

    private void ExternalApp_QueryWindow(object? sender, QueryWindowEventArgs e)
    {
        RaiseQueryWindow(e);
    }

    private void RaiseApplicationActivated()
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseApplicationActivated)));
        OnApplicationActivated();
        ApplicationActivated?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseApplicationClosed(ApplicationClosedEventArgs applicationClosedEventArgs)
    {
        if (InvokeRequired)
        {
            Invoke(RaiseApplicationClosed, applicationClosedEventArgs);
            return;
        }

        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseApplicationClosed)));
        OnApplicationClosed();
        ApplicationClosed?.Invoke(this, applicationClosedEventArgs);
    }

    private void RaiseApplicationStarted()
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseApplicationStarted)));
        OnApplicationStarted();
        ApplicationStarted?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseQueryWindow(QueryWindowEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(RaiseQueryWindow, e);
            return;
        }
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseQueryWindow)));
        OnQueryWindow(e);
        QueryWindow?.Invoke(this, e);
    }

    private void RaiseWindowTitleChanged()
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseWindowTitleChanged)));
        OnWindowTitleChanged();
        WindowTitleChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task StartAsync(ExternalAppConfiguration configuration, CancellationToken cancellationToken = default)
    {
        Logger.WithCallerInfo(log => log.LogDebug("Starting executable '{Executable}'", configuration.Executable));
        _externalApp = new ExternalApp(configuration, LoggerFactory);
        _externalApp.ProcessExited += ExternalApp_ProcessExited;
        _externalApp.QueryWindow += ExternalApp_QueryWindow;

        try
        {
            await _externalApp.StartAsync(cancellationToken);
            Logger.WithCallerInfo(log => log.LogDebug("Embedding window for '{Executable}'", configuration.Executable));
            if (!_externalApp.Configuration.StartExternal)
                await EmbedApplicationAsync(cancellationToken);

            Invoke(StartedSuccessful);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{Method} failed starting '{Executable}'",
                nameof(StartAsync), configuration.Executable);
            RaiseApplicationClosed(new ApplicationClosedEventArgs(ex));
        }
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
            if (_externalApp is not null && _externalApp.IsEmbedded)
            {
                _externalApp.SetWindowPosition(0, 0, Width, Height);
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

    private void StartedSuccessful()
    {
        if (_externalApp is null or {IsRunning: false})
            return;

        SetupHooks();
        SetWindowPosition();
        RaiseApplicationStarted();
    }

    private void SetupHooks()
    {
        if (_externalApp is null or {IsRunning: false})
            return;

        var winEventProc = new WINEVENTPROC(WinEventProc);

        // always add the delegate to the _winEventProcedures list
        // otherwise GC will dump this delegate and the hook fails
        _winEventProcedures.Add(winEventProc);
        const uint eventType = PInvoke.EVENT_OBJECT_NAMECHANGE;

        var hook = PInvoke.SetWinEventHook(
            eventType,
            eventType,
            new HINSTANCE(IntPtr.Zero),
            winEventProc,
            (uint) _externalApp.Process!.Id,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT);

        _winEventHooks.Add(hook);
    }

    // ReSharper disable once IdentifierTypo
    private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hWnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime)
    {
        // Logger.WithCallerInfo(logger => logger.LogDebug(
        //     "WinEventProc: EventType: {EventType}, Window Handle: {WindowHandle}, idObject: {IdObject}, idChild: {IdChild}",
        //     eventType, hWnd, idObject, idChild));

        if (_externalApp == null)
            return;

        if (_externalApp.WindowHandle != hWnd)
        {
            // Logger.WithCallerInfo(logger => logger.LogDebug(
            //     "WinEventProc: exiting because WindowHandle ({AppWindowHandle}) != hWnd ({WindowHandle})",
            //     _externalApp.WindowHandle, hWnd));
            return;
        }

        switch (eventType)
        {
            case PInvoke.EVENT_OBJECT_NAMECHANGE:
                Logger.WithCallerInfo(logger => logger.LogDebug("WinEventProc: EVENT_OBJECT_NAMECHANGE: {WindowTitle}",
                    _externalApp.GetWindowTitle()));
                Invoke(RaiseWindowTitleChanged);
                break;
        }
    }
}
