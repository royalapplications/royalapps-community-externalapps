using System;

namespace RoyalApps.Community.ExternalApps.WinForms.Selection;

internal enum WindowSelectionOutcome
{
    Selected,
    TimedOut,
    StartedProcessExited
}

internal sealed class WindowSelectionResult
{
    private WindowSelectionResult(WindowSelectionOutcome outcome, ExternalWindowCandidate? selectedCandidate)
    {
        Outcome = outcome;
        SelectedCandidate = selectedCandidate;
    }

    public WindowSelectionOutcome Outcome { get; }

    public ExternalWindowCandidate? SelectedCandidate { get; }

    public static WindowSelectionResult Selected(ExternalWindowCandidate selectedCandidate)
    {
        ArgumentNullException.ThrowIfNull(selectedCandidate);
        return new WindowSelectionResult(WindowSelectionOutcome.Selected, selectedCandidate);
    }

    public static WindowSelectionResult TimedOut() => new(WindowSelectionOutcome.TimedOut, null);

    public static WindowSelectionResult StartedProcessExited() =>
        new(WindowSelectionOutcome.StartedProcessExited, null);
}
