using RoyalApps.Community.ExternalApps.WinForms.Options;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.Interfaces;

internal interface IEmbeddingController : IDisposable
{
    bool IsEmbedded { get; }

    Task EmbedAsync(ExternalAppHost ownerControl, HWND windowHandle, ExternalAppEmbeddingOptions options, CancellationToken cancellationToken);

    void Detach(HWND windowHandle);

    void Focus(HWND windowHandle);

    void SetWindowPosition(HWND windowHandle, Rectangle bounds, ExternalAppEmbeddingOptions options);
}
