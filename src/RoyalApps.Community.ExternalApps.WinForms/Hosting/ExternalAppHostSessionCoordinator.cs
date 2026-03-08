using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using RoyalApps.Community.ExternalApps.WinForms.Events;
using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using RoyalApps.Community.ExternalApps.WinForms.Selection;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.Extensions;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.Hosting;

internal sealed class ExternalAppHostSessionCoordinator : IExternalAppHostSessionCoordinator
{
    private readonly Func<ILoggerFactory> _loggerFactoryProvider;
    private readonly ILogger _logger;

    public ExternalAppHostSessionCoordinator(Func<ILoggerFactory> loggerFactoryProvider, ILogger logger)
    {
        _loggerFactoryProvider = loggerFactoryProvider ?? throw new ArgumentNullException(nameof(loggerFactoryProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<ApplicationClosedEventArgs>? SessionClosed;

    public ExternalAppOptions? Options => _session?.Options;

    public Process? Process => _session?.Process;

    public HWND WindowHandle => _session?.WindowHandle ?? HWND.Null;

    public bool HasWindow => _session is { HasWindow: true };

    public AttachmentState AttachmentState => _session?.AttachmentState ?? AttachmentState.None;

    public bool IsEmbedded => _session?.IsEmbedded ?? false;

    public bool IsRunning => _session?.IsRunning ?? false;

    public bool IsExternal => _session?.IsExternal ?? false;

    private ExternalAppSession? _session;

    public async Task StartAsync(
        ExternalAppOptions options,
        Action<WindowSelectionRequestEventArgs>? raiseSelectionRequest,
        CancellationToken cancellationToken)
    {
        DisposeSession();

        _logger.WithCallerInfo(log => log.LogDebug("Starting executable '{Executable}'", options.Launch.Executable));

        var session = new ExternalAppSession(options, _loggerFactoryProvider());
        session.ProcessExited += Session_ProcessExited;
        _session = session;

        try
        {
            await session.StartAsync(raiseSelectionRequest, cancellationToken);
        }
        catch
        {
            if (ReferenceEquals(_session, session))
                DisposeSession();

            throw;
        }
    }

    public async Task EmbedAsync(ExternalAppHost ownerControl, CancellationToken cancellationToken)
    {
        if (_session is null)
            throw new MissingWindowException();

        await _session.EmbedAsync(ownerControl, cancellationToken);
    }

    public void CloseApplication() => _session?.CloseApplication();

    public void DetachApplication() => _session?.DetachApplication();

    public void FocusApplication() => _session?.FocusApplication();

    public void MarkAsExternal() => _session?.MarkAsExternal();

    public void SetWindowPosition(Rectangle rectangle) =>
        _session?.SetWindowPosition(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    public string GetWindowTitle() => _session?.GetWindowTitle() ?? string.Empty;

    public void Dispose() => DisposeSession();

    private void DisposeSession()
    {
        if (_session == null)
            return;

        try
        {
            _session.ProcessExited -= Session_ProcessExited;
            _session.Dispose();
        }
        catch
        {
            // Ignore shutdown races.
        }
        finally
        {
            _session = null;
        }
    }

    private void Session_ProcessExited(object? sender, EventArgs e) =>
        SessionClosed?.Invoke(this, new ApplicationClosedEventArgs { ProcessExited = true });
}
