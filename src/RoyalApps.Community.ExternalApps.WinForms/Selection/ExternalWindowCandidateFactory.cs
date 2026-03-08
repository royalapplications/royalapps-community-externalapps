using RoyalApps.Community.ExternalApps.WinForms.Discovery;
using System;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.Selection;

internal sealed class ExternalWindowCandidateFactory
{
    private readonly EmbeddingCompatibilityAssessor _compatibilityAssessor;

    public ExternalWindowCandidateFactory(EmbeddingCompatibilityAssessor? compatibilityAssessor = null)
    {
        _compatibilityAssessor = compatibilityAssessor ?? new EmbeddingCompatibilityAssessor();
    }

    public ExternalWindowCandidate Create(HWND windowHandle, ProcessSnapshot processSnapshot)
    {
        ArgumentNullException.ThrowIfNull(processSnapshot);
        var className = NativeWindowUtilities.GetClassName(windowHandle);
        var provisionalCandidate = new ExternalWindowCandidate
        {
            WindowHandle = windowHandle,
            ProcessId = processSnapshot.ProcessId,
            ProcessName = processSnapshot.ProcessName,
            ExecutablePath = processSnapshot.ExecutablePath,
            CommandLine = processSnapshot.CommandLine,
            ClassName = className,
            WindowTitle = NativeWindowUtilities.GetWindowTitle(windowHandle),
            IsVisible = true,
            IsTopLevel = NativeWindowUtilities.IsTopLevelWindow(windowHandle),
        };
        var assessment = _compatibilityAssessor.Assess(provisionalCandidate);

        return new ExternalWindowCandidate
        {
            WindowHandle = provisionalCandidate.WindowHandle,
            ProcessId = provisionalCandidate.ProcessId,
            ProcessName = provisionalCandidate.ProcessName,
            ExecutablePath = provisionalCandidate.ExecutablePath,
            CommandLine = provisionalCandidate.CommandLine,
            ClassName = provisionalCandidate.ClassName,
            WindowTitle = provisionalCandidate.WindowTitle,
            IsVisible = provisionalCandidate.IsVisible,
            IsTopLevel = provisionalCandidate.IsTopLevel,
            PrefersExternalHosting = assessment.PrefersExternalHosting,
            EmbeddingCompatibilityWarning = assessment.Warning,
        };
    }
}
