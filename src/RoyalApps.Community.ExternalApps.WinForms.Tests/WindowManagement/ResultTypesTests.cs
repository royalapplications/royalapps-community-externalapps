using System;
using System.Diagnostics;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class ResultTypesTests
{
    [Fact]
    public void WindowSelectionResult_Selected_PreservesSelectedCandidate()
    {
        var candidate = CreateCandidate((IntPtr)0x6001, 6001);

        var result = WindowSelectionResult.Selected(candidate);

        Assert.Equal(WindowSelectionOutcome.Selected, result.Outcome);
        Assert.Same(candidate, result.SelectedCandidate);
    }

    [Fact]
    public void WindowSelectionResult_TimedOut_HasNoSelectedCandidate()
    {
        var result = WindowSelectionResult.TimedOut();

        Assert.Equal(WindowSelectionOutcome.TimedOut, result.Outcome);
        Assert.Null(result.SelectedCandidate);
    }

    [Fact]
    public void WindowSelectionResult_StartedProcessExited_HasNoSelectedCandidate()
    {
        var result = WindowSelectionResult.StartedProcessExited();

        Assert.Equal(WindowSelectionOutcome.StartedProcessExited, result.Outcome);
        Assert.Null(result.SelectedCandidate);
    }

    [Fact]
    public void ProcessLaunchResult_Started_PreservesProcess()
    {
        using var process = Process.GetCurrentProcess();

        var result = ProcessLaunchResult.Started(process);

        Assert.Equal(ProcessLaunchOutcome.Started, result.Outcome);
        Assert.Same(process, result.Process);
    }

    [Fact]
    public void ProcessLaunchResult_ExistingProcess_HasNoProcessInstance()
    {
        var result = ProcessLaunchResult.ExistingProcess();

        Assert.Equal(ProcessLaunchOutcome.ExistingProcess, result.Outcome);
        Assert.Null(result.Process);
    }

    [Fact]
    public void ProcessLaunchResult_LauncherExitedBeforeWindowDiscovery_HasNoProcessInstance()
    {
        var result = ProcessLaunchResult.LauncherExitedBeforeWindowDiscovery();

        Assert.Equal(ProcessLaunchOutcome.LauncherExitedBeforeWindowDiscovery, result.Outcome);
        Assert.Null(result.Process);
    }

    private static ExternalWindowCandidate CreateCandidate(IntPtr handle, int processId)
    {
        return new ExternalWindowCandidate
        {
            WindowHandle = handle,
            ProcessId = processId,
            ProcessName = "TestProcess",
            ExecutablePath = @"C:\Test\TestProcess.exe",
            CommandLine = "\"C:\\Test\\TestProcess.exe\"",
            ClassName = "TestWindowClass",
            WindowTitle = "Test Window",
            IsVisible = true,
            IsTopLevel = true
        };
    }
}
