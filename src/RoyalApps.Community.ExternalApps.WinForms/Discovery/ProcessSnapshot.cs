namespace RoyalApps.Community.ExternalApps.WinForms.Discovery;

internal sealed record ProcessSnapshot(
    int ProcessId,
    string ProcessName,
    string ExecutablePath,
    string CommandLine);
