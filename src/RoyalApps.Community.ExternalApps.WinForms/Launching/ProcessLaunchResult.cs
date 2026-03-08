using System;
using System.Diagnostics;

namespace RoyalApps.Community.ExternalApps.WinForms.Launching;

internal enum ProcessLaunchOutcome
{
    Started,
    ExistingProcess,
    LauncherExitedBeforeWindowDiscovery
}

internal sealed class ProcessLaunchResult
{
    private ProcessLaunchResult(ProcessLaunchOutcome outcome, Process? process)
    {
        Outcome = outcome;
        Process = process;
    }

    public ProcessLaunchOutcome Outcome { get; }

    public Process? Process { get; }

    public static ProcessLaunchResult Started(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        return new ProcessLaunchResult(ProcessLaunchOutcome.Started, process);
    }

    public static ProcessLaunchResult ExistingProcess() =>
        new(ProcessLaunchOutcome.ExistingProcess, null);

    public static ProcessLaunchResult LauncherExitedBeforeWindowDiscovery() =>
        new(ProcessLaunchOutcome.LauncherExitedBeforeWindowDiscovery, null);
}
