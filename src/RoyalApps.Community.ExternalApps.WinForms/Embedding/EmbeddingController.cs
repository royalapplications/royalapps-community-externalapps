using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.Embedding;

internal sealed class EmbeddingController : IEmbeddingController
{
    private readonly ControlEmbeddingStrategy _controlEmbeddingStrategy;
    private readonly WindowEmbeddingStrategy _windowEmbeddingStrategy;
    private IEmbeddingStrategy? _activeStrategy;

    public EmbeddingController(ILogger<EmbeddingController> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _controlEmbeddingStrategy = new ControlEmbeddingStrategy(logger);
        _windowEmbeddingStrategy = new WindowEmbeddingStrategy(logger);
    }

    public bool IsEmbedded { get; private set; }

    public EmbedMethod Mode { get; private set; }

    public void Dispose() => _activeStrategy?.Dispose();

    public async Task EmbedAsync(ExternalAppHost ownerControl, HWND windowHandle, ExternalAppEmbeddingOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var strategy = GetStrategy(options.Mode);
        if (_activeStrategy != null && !ReferenceEquals(_activeStrategy, strategy))
            _activeStrategy.Detach(windowHandle);

        Mode = options.Mode;
        _activeStrategy = strategy;
        await strategy.EmbedAsync(ownerControl, windowHandle, options, cancellationToken);
        IsEmbedded = true;
    }

    public void Detach(HWND windowHandle)
    {
        if (windowHandle == HWND.Null)
            return;

        _activeStrategy?.Detach(windowHandle);
        IsEmbedded = false;
    }

    public void Focus(HWND windowHandle)
    {
        if (windowHandle == HWND.Null)
            return;

        PInvoke.SetForegroundWindow(windowHandle);
        PInvoke.SetFocus(windowHandle);
        PInvoke.BringWindowToTop(windowHandle);
    }

    public void SetWindowPosition(HWND windowHandle, Rectangle bounds, ExternalAppEmbeddingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (windowHandle == HWND.Null)
            return;

        if (!IsEmbedded)
        {
            PInvoke.MoveWindow(windowHandle, bounds.X, bounds.Y, bounds.Width, bounds.Height, true);
            return;
        }

        _activeStrategy?.SetWindowPosition(windowHandle, bounds, true, options);
    }

    private IEmbeddingStrategy GetStrategy(EmbedMethod mode)
    {
        return mode switch
        {
            EmbedMethod.Control => _controlEmbeddingStrategy,
            EmbedMethod.Window => _windowEmbeddingStrategy,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported embed method.")
        };
    }
}
