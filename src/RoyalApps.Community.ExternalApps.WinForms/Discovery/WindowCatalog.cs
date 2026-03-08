using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.Discovery;

internal sealed class WindowCatalog : IWindowCatalog
{
    private readonly ILogger<WindowCatalog> _logger;
    private readonly ProcessMetadataProvider _processMetadataProvider;
    private readonly ExternalWindowCandidateFactory _candidateFactory;

    public WindowCatalog(
        ILogger<WindowCatalog> logger,
        ProcessMetadataProvider processMetadataProvider,
        ExternalWindowCandidateFactory candidateFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processMetadataProvider = processMetadataProvider ?? throw new ArgumentNullException(nameof(processMetadataProvider));
        _candidateFactory = candidateFactory ?? throw new ArgumentNullException(nameof(candidateFactory));
    }

    public IReadOnlyList<ExternalWindowCandidate> GetAvailableWindows()
    {
        var commandLines = _processMetadataProvider.LoadCommandLines();
        var candidates = new List<ExternalWindowCandidate>();
        var currentProcessId = Process.GetCurrentProcess().Id;

        BOOL Filter(HWND hWnd, LPARAM lParam)
        {
            try
            {
                if (!PInvoke.IsWindowVisible(hWnd))
                    return true;

                if (ExternalApps.IsTrackedWindow(hWnd))
                    return true;

                var processId = NativeWindowUtilities.GetProcessId(hWnd);
                if (processId is null || processId.Value == currentProcessId)
                    return true;

                var processSnapshot = _processMetadataProvider.TryGetSnapshot(processId.Value, commandLines);
                if (processSnapshot == null)
                    return true;

                candidates.Add(_candidateFactory.Create(hWnd, processSnapshot));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Enumerating available windows failed");
            }

            return true;
        }

        var result = PInvoke.EnumDesktopWindows(new Windows.Win32.System.StationsAndDesktops.HDESK(IntPtr.Zero), Filter, new LPARAM(IntPtr.Zero));
        if (!result)
            _logger.LogWarning("Unable to enumerate desktop windows");

        return candidates
            .OrderBy(candidate => candidate.ProcessId)
            .ThenBy(candidate => candidate.WindowTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
