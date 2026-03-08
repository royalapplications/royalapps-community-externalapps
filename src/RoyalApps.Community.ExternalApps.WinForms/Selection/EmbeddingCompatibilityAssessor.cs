using System;
using System.Collections.Generic;
using System.Linq;

namespace RoyalApps.Community.ExternalApps.WinForms.Selection;

internal sealed class EmbeddingCompatibilityAssessor
{
    private static readonly string[] RiskyWindowClasses =
    [
        "ApplicationFrameWindow",
        "WinUIDesktopWin32WindowClass",
        "Windows.UI.Composition.DesktopWindowContentBridge"
    ];

    public EmbeddingCompatibilityAssessment Assess(ExternalWindowCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var signals = new List<string>();

        if (LooksPackaged(candidate.ExecutablePath))
            signals.Add("the executable is deployed from the WindowsApps package store");

        if (LooksModernWindowClass(candidate.ClassName))
            signals.Add($"the native window class '{candidate.ClassName}' is associated with modern desktop app hosting");

        if (LooksPackagedCommandLine(candidate.CommandLine))
            signals.Add("the command line suggests packaged app activation");

        if (signals.Count == 0)
            return EmbeddingCompatibilityAssessment.Default;

        var warning = $"This window appears to come from a modern or packaged desktop app because {JoinSignals(signals)}. " +
                      "Reparenting via Control or Window embedding may be unstable. Prefer a non-reparented external-hosting mode when possible.";

        return new EmbeddingCompatibilityAssessment(true, warning);
    }

    private static bool LooksPackaged(string executablePath)
    {
        return !string.IsNullOrWhiteSpace(executablePath) &&
               executablePath.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksModernWindowClass(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return false;

        return RiskyWindowClasses.Contains(className, StringComparer.OrdinalIgnoreCase) ||
               className.Contains("WinUI", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("Xaml", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("ApplicationFrame", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksPackagedCommandLine(string commandLine)
    {
        return !string.IsNullOrWhiteSpace(commandLine) &&
               (commandLine.Contains("AppX", StringComparison.OrdinalIgnoreCase) ||
                commandLine.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase));
    }

    private static string JoinSignals(IReadOnlyList<string> signals)
    {
        return signals.Count switch
        {
            1 => signals[0],
            2 => $"{signals[0]} and {signals[1]}",
            _ => $"{string.Join(", ", signals.Take(signals.Count - 1))}, and {signals[^1]}"
        };
    }
}
