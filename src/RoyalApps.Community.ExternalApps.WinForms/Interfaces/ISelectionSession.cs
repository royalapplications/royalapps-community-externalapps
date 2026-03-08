using RoyalApps.Community.ExternalApps.WinForms.Events;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using RoyalApps.Community.ExternalApps.WinForms.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RoyalApps.Community.ExternalApps.WinForms.Interfaces;

internal interface ISelectionSession
{
    Task<WindowSelectionResult> SelectWindowAsync(
        Process? startedProcess,
        string? requestedExecutablePath,
        IReadOnlySet<nint>? baselineWindowHandles,
        ExternalAppSelectionOptions options,
        Action<WindowSelectionRequestEventArgs>? raiseSelectionRequest,
        CancellationToken cancellationToken);

    IReadOnlySet<nint> CaptureKnownWindowHandles();
}
