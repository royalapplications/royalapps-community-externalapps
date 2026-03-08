using RoyalApps.Community.ExternalApps.WinForms.Selection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RoyalApps.Community.ExternalApps.WinForms.Events;

/// <summary>
/// Provides the currently available candidate windows and allows the host to choose one.
/// </summary>
public sealed class WindowSelectionRequestEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSelectionRequestEventArgs"/> class.
    /// </summary>
    /// <param name="candidates">The currently available windows that may be selected for embedding.</param>
    /// <param name="elapsed">The elapsed selection time.</param>
    /// <param name="timeout">The configured selection timeout.</param>
    /// <param name="startedProcessId">The identifier of the process started by the session, if a new process was launched.</param>
    /// <param name="requestedExecutablePath">The executable path requested by the session, if one was configured.</param>
    /// <param name="newlyDiscoveredCandidates">The subset of candidates that appeared for the first time during the current selection session.</param>
    public WindowSelectionRequestEventArgs(
        IReadOnlyList<ExternalWindowCandidate> candidates,
        TimeSpan elapsed,
        TimeSpan timeout,
        int? startedProcessId,
        string? requestedExecutablePath,
        IReadOnlyList<ExternalWindowCandidate> newlyDiscoveredCandidates)
    {
        Candidates = new ReadOnlyCollection<ExternalWindowCandidate>(candidates.ToList());
        Elapsed = elapsed;
        Timeout = timeout;
        StartedProcessId = startedProcessId;
        RequestedExecutablePath = requestedExecutablePath;
        NewlyDiscoveredCandidates = new ReadOnlyCollection<ExternalWindowCandidate>(newlyDiscoveredCandidates.ToList());
    }

    /// <summary>
    /// Gets the current candidate windows.
    /// </summary>
    public IReadOnlyList<ExternalWindowCandidate> Candidates { get; }

    /// <summary>
    /// Gets the time elapsed since selection started.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Gets the maximum time allowed for selection.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Gets the identifier of the process started by the session, if one was launched.
    /// </summary>
    public int? StartedProcessId { get; }

    /// <summary>
    /// Gets the executable path requested by the session, if one was configured.
    /// </summary>
    public string? RequestedExecutablePath { get; }

    /// <summary>
    /// Gets the candidates that appeared for the first time during the current selection session.
    /// </summary>
    public IReadOnlyList<ExternalWindowCandidate> NewlyDiscoveredCandidates { get; }

    /// <summary>
    /// Gets the handle of the selected window, or <see cref="IntPtr.Zero"/> when no window has been selected yet.
    /// </summary>
    public IntPtr SelectedWindowHandle { get; private set; }

    /// <summary>
    /// Selects a candidate window by handle.
    /// </summary>
    /// <param name="windowHandle">The handle of the window to embed.</param>
    public void SelectWindow(IntPtr windowHandle) => SelectedWindowHandle = windowHandle;

    /// <summary>
    /// Selects a candidate window from the current candidate list.
    /// </summary>
    /// <param name="candidate">The candidate to embed.</param>
    public void SelectWindow(ExternalWindowCandidate candidate) => SelectedWindowHandle = candidate.WindowHandle;
}
