using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using RoyalApps.Community.ExternalApps.WinForms.Events;
using RoyalApps.Community.ExternalApps.WinForms.Hosting;
using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using RoyalApps.Community.ExternalApps.WinForms.Selection;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RoyalApps.Community.ExternalApps.WinForms.Extensions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// The host control which can embed external application windows.
/// </summary>
public class ExternalAppHost : Control
{
    private readonly IExternalAppHostSessionCoordinator _sessionCoordinator;
    private readonly ExternalAppHostUiDispatcher _uiDispatcher;
    private readonly WindowHookManager _windowHookManager;

    private bool IsLeftMouseButtonDown => MouseButtons.HasFlag(MouseButtons.Left);

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal HWND ControlHandle { get; private set; }

    /// <summary>
    /// Gets or sets the logger factory used to create host loggers.
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// Gets the logger instance used by the host.
    /// </summary>
    protected ILogger Logger
    {
        get
        {
            field ??= LoggerFactory.CreateLogger<ExternalAppHost>();
            return field;
        }
    }

    /// <summary>
    /// Gets the handle of the currently tracked external window, or <see cref="IntPtr.Zero"/> when no window is attached.
    /// </summary>
    public IntPtr EmbeddedWindowHandle => _sessionCoordinator.WindowHandle;

    /// <summary>
    /// Gets the options used by the current session, or <see langword="null"/> when no session is active.
    /// </summary>
    public ExternalAppOptions? Options => _sessionCoordinator.Options;

    /// <summary>
    /// Gets how the current external window is attached to the host.
    /// </summary>
    public AttachmentState AttachmentState => _sessionCoordinator.AttachmentState;

    /// <summary>
    /// Gets a value indicating whether the current window is embedded in the host control.
    /// </summary>
    public bool IsEmbedded => _sessionCoordinator.IsEmbedded;

    /// <summary>
    /// Gets the tracked process for the current session, or <see langword="null"/> when no process is attached.
    /// </summary>
    public Process? Process => _sessionCoordinator.Process;

    /// <summary>
    /// Occurs when the embedded application receives focus.
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationActivated;

    /// <summary>
    /// Occurs when the tracked application closes or the session terminates with an error.
    /// </summary>
    public event EventHandler<ApplicationClosedEventArgs>? ApplicationClosed;

    /// <summary>
    /// Occurs after the application has started and any initial embedding work has completed.
    /// </summary>
    public event EventHandler<EventArgs>? ApplicationStarted;

    /// <summary>
    /// Occurs while candidate windows are being discovered and allows the consumer to choose the window to embed.
    /// </summary>
    public event EventHandler<WindowSelectionRequestEventArgs>? WindowSelectionRequested;

    /// <summary>
    /// Occurs when the tracked window caption changes.
    /// </summary>
    public event EventHandler<WindowCaptionEventArgs>? WindowTitleChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalAppHost"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory used for host and session logging. When <see langword="null"/>, <see cref="NullLoggerFactory.Instance"/> is used.</param>
    public ExternalAppHost(ILoggerFactory? loggerFactory = null)
    {
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _uiDispatcher = new ExternalAppHostUiDispatcher(this);
        var hostLogger = LoggerFactory.CreateLogger<ExternalAppHost>();
        _sessionCoordinator = new ExternalAppHostSessionCoordinator(() => LoggerFactory, hostLogger);
        _sessionCoordinator.SessionClosed += SessionCoordinator_SessionClosed;
        _windowHookManager = new WindowHookManager(hostLogger);
        SetStyle(ControlStyles.ContainerControl, false);
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    internal ExternalAppHost(
        IExternalAppHostSessionCoordinator sessionCoordinator,
        ILoggerFactory? loggerFactory = null,
        WindowHookManager? windowHookManager = null)
    {
        _sessionCoordinator = sessionCoordinator ?? throw new ArgumentNullException(nameof(sessionCoordinator));
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _uiDispatcher = new ExternalAppHostUiDispatcher(this);
        _windowHookManager = windowHookManager ?? new WindowHookManager(LoggerFactory.CreateLogger<ExternalAppHost>());
        _sessionCoordinator.SessionClosed += SessionCoordinator_SessionClosed;
        SetStyle(ControlStyles.ContainerControl, false);
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    /// <summary>
    /// Requests that the tracked application is closed.
    /// </summary>
    /// <remarks>
    /// Depending on the application, this may show confirmation dialogs. If <see cref="ExternalAppLaunchOptions.KillOnClose"/> is enabled,
    /// the process is terminated when graceful shutdown fails.
    /// </remarks>
    public void CloseApplication() => _sessionCoordinator.CloseApplication();

    /// <summary>
    /// Detaches the tracked application window from the host control.
    /// </summary>
    public void DetachApplication()
    {
        _sessionCoordinator.DetachApplication();
        SetWindowPosition();
    }

    /// <summary>
    /// Re-embeds a previously detached application window.
    /// </summary>
    public void EmbedApplication()
    {
        if (!_sessionCoordinator.HasWindow)
            return;

        _ = Task.Run(() => EmbedApplicationAsync(CancellationToken.None));
    }

    /// <summary>
    /// Starts a new external application session with the specified options.
    /// </summary>
    /// <param name="options">The runtime options that control launch, selection, and embedding behavior.</param>
    /// <remarks>
    /// This method schedules the startup workflow on a background task and returns immediately. Subscribe to
    /// <see cref="ApplicationStarted"/>, <see cref="ApplicationClosed"/>, and <see cref="WindowSelectionRequested"/> to observe progress.
    /// The control handle must already exist before calling this method.
    /// </remarks>
    public void Start(ExternalAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!IsHandleCreated)
        {
            throw new InvalidOperationException(
                "ExternalAppHost requires a created window handle before Start can be called. Ensure the control is created and shown before starting an external application.");
        }

        _ = Task.Run(() => StartAsync(options));
    }

    /// <summary>
    /// Displays the system menu of the tracked window at the specified control-relative location.
    /// </summary>
    /// <param name="location">The location, in control coordinates, where the menu should be shown.</param>
    public void ShowSystemMenu(Point location)
    {
        if (!_sessionCoordinator.HasWindow)
            return;

        ExternalApps.ShowSystemMenu(_sessionCoordinator.WindowHandle, ControlHandle, PointToScreen(location));
    }

    /// <summary>
    /// Transfers focus to the tracked application.
    /// </summary>
    /// <param name="force"><see langword="true"/> to focus even when the window is detached; otherwise, focus is applied only while embedded.</param>
    public void FocusApplication(bool force) => OnFocusApplication(force);

    /// <summary>
    /// Captures a bitmap of the currently tracked window.
    /// </summary>
    /// <returns>A bitmap of the client area, or <see langword="null"/> when no valid window is available.</returns>
    public Bitmap? GetWindowScreenshot()
    {
        if (!_sessionCoordinator.HasWindow)
            return null;

        PInvoke.GetWindowRect(_sessionCoordinator.WindowHandle, out var rect);
        if (rect.right == 0 || rect.bottom == 0)
            return null;

        var bmp = new Bitmap(rect.right, rect.bottom, PixelFormat.Format32bppArgb);
        using var gfxBmp = Graphics.FromImage(bmp);
        var hdcBitmap = gfxBmp.GetHdc();
        var hdc = new HDC(hdcBitmap);

        PInvoke.PrintWindow(
            _sessionCoordinator.WindowHandle,
            hdc,
            Windows.Win32.Storage.Xps.PRINT_WINDOW_FLAGS.PW_CLIENTONLY);

        gfxBmp.ReleaseHdc(hdcBitmap);
        return bmp;
    }

    /// <summary>
    /// Maximizes the tracked application window.
    /// </summary>
    public void MaximizeApplication()
    {
        if (!_sessionCoordinator.HasWindow)
            return;

        PInvoke.ShowWindow(_sessionCoordinator.WindowHandle, SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED);
    }

    /// <summary>
    /// Moves and resizes the tracked application window to the specified bounds.
    /// </summary>
    /// <param name="rectangle">The target bounds, in host coordinates when embedded or screen coordinates when detached.</param>
    public void SetWindowPosition(Rectangle rectangle) => _sessionCoordinator.SetWindowPosition(rectangle);

    /// <summary>
    /// Raises the <see cref="ApplicationActivated"/> event.
    /// </summary>
    protected virtual void OnApplicationActivated() { }

    /// <summary>
    /// Raises the <see cref="ApplicationClosed"/> event.
    /// </summary>
    protected virtual void OnApplicationClosed() { }

    /// <summary>
    /// Raises the <see cref="ApplicationStarted"/> event.
    /// </summary>
    protected virtual void OnApplicationStarted() { }

    /// <summary>
    /// Raises the <see cref="WindowSelectionRequested"/> event.
    /// </summary>
    /// <param name="e">The selection request that contains the current candidate windows.</param>
    protected virtual void OnWindowSelectionRequested(WindowSelectionRequestEventArgs e) { }

    /// <summary>
    /// Raises the <see cref="WindowTitleChanged"/> event.
    /// </summary>
    /// <param name="e">The event arguments that contain the new caption.</param>
    protected virtual void OnWindowTitleChanged(WindowCaptionEventArgs e) { }

    /// <summary>
    /// Focuses the host and the tracked application window.
    /// </summary>
    /// <param name="force"><see langword="true"/> to focus detached windows as well; otherwise only embedded windows are focused.</param>
    protected virtual void OnFocusApplication(bool force)
    {
        if (!_sessionCoordinator.HasWindow)
            return;

        if (!_sessionCoordinator.IsEmbedded && !force)
            return;

        _uiDispatcher.InvokeIfRequired(() => Focus());
        _sessionCoordinator.FocusApplication();
    }

    /// <summary>
    /// Releases managed and unmanaged resources used by the host.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed state; otherwise only unmanaged cleanup is performed.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sessionCoordinator.SessionClosed -= SessionCoordinator_SessionClosed;
            _sessionCoordinator.Dispose();
            _windowHookManager.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Captures the created control handle for native embedding operations.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ControlHandle = new HWND(Handle);
    }

    /// <summary>
    /// Repositions the tracked window when the host control changes size.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        SetWindowPosition();
    }

    /// <summary>
    /// Processes focus-related window messages to keep the tracked application synchronized with the host.
    /// </summary>
    /// <param name="m">The current Windows message.</param>
    protected override void WndProc(ref Message m)
    {
        switch ((uint)m.Msg)
        {
            case PInvoke.WM_SETFOCUS:
                FocusApplication(false);
                break;
            case PInvoke.WM_MOUSEACTIVATE or PInvoke.WM_LBUTTONDOWN or PInvoke.WM_MDIACTIVATE when !IsLeftMouseButtonDown:
                FocusApplication(false);
                break;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// Repositions the tracked application window to match the current host bounds.
    /// </summary>
    public void SetWindowPosition()
    {
        if (Disposing || IsDisposed || Options == null)
            return;

        try
        {
            if (_sessionCoordinator.IsEmbedded)
            {
                _sessionCoordinator.SetWindowPosition(new Rectangle(0, 0, Width, Height));
            }
            else if (_sessionCoordinator.HasWindow)
            {
                var screenPoint = PointToScreen(Point.Empty);
                _sessionCoordinator.SetWindowPosition(new Rectangle(screenPoint.X, screenPoint.Y, Width, Height));
                PInvoke.ShowWindow(_sessionCoordinator.WindowHandle, SHOW_WINDOW_CMD.SW_SHOWDEFAULT);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Cannot set the window position");
        }
    }

    private async Task StartAsync(ExternalAppOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await _sessionCoordinator.StartAsync(options, RaiseWindowSelectionRequested, cancellationToken);

            if (_sessionCoordinator.HasWindow &&
                options.Embedding.StartEmbedded)
            {
                Logger.WithCallerInfo(log => log.LogDebug("Embedding window for '{Executable}'", options.Launch.Executable));
                try
                {
                    await EmbedApplicationAsync(cancellationToken);
                }
                catch (EmbeddingFailedException ex)
                {
                    _sessionCoordinator.MarkAsExternal();
                    Logger.LogWarning(
                        ex,
                        "Embedding '{Executable}' with mode {Mode} failed. Leaving the window external. Consider a non-reparented external-hosting mode for modern or packaged desktop apps.",
                        options.Launch.Executable,
                        options.Embedding.Mode);
                    SetWindowPosition();
                }
            }

            await _uiDispatcher.InvokeAsync(StartedSuccessful, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{Method} failed starting '{Executable}'", nameof(StartAsync), options.Launch.Executable);
            RaiseApplicationClosed(new ApplicationClosedEventArgs(ex));
        }
    }

    private async Task EmbedApplicationAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionCoordinator.HasWindow)
            throw new MissingWindowException();

        await _uiDispatcher.InvokeAsync(async embedCancellationToken =>
        {
            await _sessionCoordinator.EmbedAsync(this, embedCancellationToken);
            SetWindowPosition();
            FocusApplication(false);
        }, cancellationToken);
    }

    private void StartedSuccessful()
    {
        if (!_sessionCoordinator.IsRunning)
            return;

        _windowHookManager.Reset(
            _sessionCoordinator.Process,
            _sessionCoordinator.GetWindowTitle,
            caption => _uiDispatcher.InvokeIfRequired(() => RaiseWindowTitleChangedCore(caption)),
            () => _uiDispatcher.InvokeIfRequired(RaiseApplicationActivatedCore));
        SetWindowPosition();
        RaiseApplicationStarted();
    }

    private void SessionCoordinator_SessionClosed(object? sender, ApplicationClosedEventArgs e)
    {
        _windowHookManager.Dispose();
        RaiseApplicationClosed(e);
    }

    private void RaiseApplicationActivatedCore()
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseApplicationActivatedCore)));
        OnApplicationActivated();
        ApplicationActivated?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseApplicationClosed(ApplicationClosedEventArgs applicationClosedEventArgs) =>
        _uiDispatcher.InvokeIfRequired(() => RaiseApplicationClosedCore(applicationClosedEventArgs));

    private void RaiseApplicationClosedCore(ApplicationClosedEventArgs applicationClosedEventArgs)
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseApplicationClosedCore)));
        OnApplicationClosed();
        ApplicationClosed?.Invoke(this, applicationClosedEventArgs);
    }

    private void RaiseApplicationStarted()
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseApplicationStarted)));
        OnApplicationStarted();
        ApplicationStarted?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseWindowSelectionRequested(WindowSelectionRequestEventArgs e) =>
        _uiDispatcher.InvokeIfRequired(() => RaiseWindowSelectionRequestedCore(e));

    private void RaiseWindowSelectionRequestedCore(WindowSelectionRequestEventArgs e)
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseWindowSelectionRequestedCore)));
        OnWindowSelectionRequested(e);
        WindowSelectionRequested?.Invoke(this, e);
    }

    private void RaiseWindowTitleChangedCore(string caption)
    {
        Logger.WithCallerInfo(logger => logger.LogDebug(nameof(RaiseWindowTitleChangedCore)));
        var e = new WindowCaptionEventArgs(caption);
        OnWindowTitleChanged(e);
        WindowTitleChanged?.Invoke(this, e);
    }
}
