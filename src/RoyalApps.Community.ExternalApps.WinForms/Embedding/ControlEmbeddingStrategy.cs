using System;
using Microsoft.Extensions.Logging;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.Embedding;

internal sealed class ControlEmbeddingStrategy : EmbeddingStrategyBase
{
    public ControlEmbeddingStrategy(ILogger<EmbeddingController> logger)
        : base(logger)
    {
    }

    protected override HWND EnsureParentWindow(ExternalAppHost ownerControl)
    {
        ArgumentNullException.ThrowIfNull(ownerControl);
        return ownerControl.ControlHandle;
    }
}
