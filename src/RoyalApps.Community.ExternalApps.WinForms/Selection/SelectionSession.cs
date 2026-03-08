using RoyalApps.Community.ExternalApps.WinForms.Events;
using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.Selection;

internal sealed class SelectionSession : ISelectionSession
{
    private readonly IWindowCatalog _windowCatalog;
    private readonly ILogger<SelectionSession> _logger;

    public SelectionSession(IWindowCatalog windowCatalog, ILogger<SelectionSession> logger)
    {
        _windowCatalog = windowCatalog ?? throw new ArgumentNullException(nameof(windowCatalog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WindowSelectionResult> SelectWindowAsync(
        Process? startedProcess,
        string? requestedExecutablePath,
        IReadOnlySet<nint>? baselineWindowHandles,
        ExternalAppSelectionOptions options,
        Action<WindowSelectionRequestEventArgs>? raiseSelectionRequest,
        CancellationToken cancellationToken)
    {
        var timeout = options.Timeout > TimeSpan.Zero
            ? options.Timeout
            : TimeSpan.FromSeconds(10);
        var pollInterval = options.PollInterval > TimeSpan.Zero
            ? options.PollInterval
            : TimeSpan.FromMilliseconds(250);

        var stopwatch = Stopwatch.StartNew();
        var seenWindowHandles = baselineWindowHandles != null
            ? new HashSet<nint>(baselineWindowHandles)
            : [];

        _logger.LogDebug(
            "Window selection started with {BaselineCount} baseline windows",
            seenWindowHandles.Count);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidates = _windowCatalog.GetAvailableWindows();
            var newlyDiscoveredCandidates = candidates
                .Where(candidate => seenWindowHandles.Add(candidate.WindowHandle))
                .ToList();

            _logger.LogDebug(
                "Window selection poll found {CandidateCount} candidates, {NewCandidateCount} newly discovered, after {ElapsedMilliseconds} ms",
                candidates.Count,
                newlyDiscoveredCandidates.Count,
                stopwatch.ElapsedMilliseconds);

            var eventArgs = new WindowSelectionRequestEventArgs(
                candidates,
                stopwatch.Elapsed,
                timeout,
                startedProcess?.Id,
                requestedExecutablePath,
                newlyDiscoveredCandidates);

            raiseSelectionRequest?.Invoke(eventArgs);

            if (eventArgs.SelectedWindowHandle != IntPtr.Zero)
            {
                var selected = candidates.FirstOrDefault(candidate => candidate.WindowHandle == eventArgs.SelectedWindowHandle);
                if (selected != null)
                    return WindowSelectionResult.Selected(selected);

                _logger.LogDebug("A consumer selected window handle {WindowHandle} which is not in the current candidate list", eventArgs.SelectedWindowHandle);
            }

            if (startedProcess is { HasExited: true })
            {
                _logger.LogDebug("Window selection stopped because the started process exited before a selection was made");
                return WindowSelectionResult.StartedProcessExited();
            }

            if (stopwatch.Elapsed >= timeout)
            {
                _logger.LogDebug(
                    "Window selection timed out after {ElapsedMilliseconds} ms with {CandidateCount} candidates in the last poll",
                    stopwatch.ElapsedMilliseconds,
                    candidates.Count);
                return WindowSelectionResult.TimedOut();
            }

            await Task.Delay(pollInterval, cancellationToken);
        }
    }

    public IReadOnlySet<nint> CaptureKnownWindowHandles()
    {
        return _windowCatalog
            .GetAvailableWindows()
            .Select(candidate => candidate.WindowHandle)
            .ToHashSet();
    }
}
