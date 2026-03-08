using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using RoyalApps.Community.ExternalApps.WinForms.Events;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.Interfaces;

internal interface IExternalAppHostSessionCoordinator : IDisposable
{
    event EventHandler<ApplicationClosedEventArgs>? SessionClosed;

    ExternalAppOptions? Options { get; }

    Process? Process { get; }

    HWND WindowHandle { get; }

    bool HasWindow { get; }

    AttachmentState AttachmentState { get; }

    bool IsEmbedded { get; }

    bool IsRunning { get; }

    bool IsExternal { get; }

    Task StartAsync(
        ExternalAppOptions options,
        Action<WindowSelectionRequestEventArgs>? raiseSelectionRequest,
        CancellationToken cancellationToken);

    Task EmbedAsync(ExternalAppHost ownerControl, CancellationToken cancellationToken);

    void CloseApplication();

    void DetachApplication();

    void FocusApplication();

    void MarkAsExternal();

    void SetWindowPosition(Rectangle rectangle);

    string GetWindowTitle();
}
