using System;
using System.Collections.Generic;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class WindowSelectionRequestEventArgsTests
{
    [Fact]
    public void Constructor_CopiesCandidatesIntoReadOnlyList()
    {
        var originalCandidates = new List<ExternalWindowCandidate>
        {
            CreateCandidate((IntPtr)0x4001, 4001)
        };

        var eventArgs = new WindowSelectionRequestEventArgs(
            originalCandidates,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            startedProcessId: 42,
            requestedExecutablePath: @"C:\Windows\System32\notepad.exe",
            newlyDiscoveredCandidates: originalCandidates);

        originalCandidates.Clear();

        Assert.Single(eventArgs.Candidates);
        Assert.Equal(@"C:\Windows\System32\notepad.exe", eventArgs.RequestedExecutablePath);
        Assert.Single(eventArgs.NewlyDiscoveredCandidates);
        Assert.Throws<NotSupportedException>(() => ((ICollection<ExternalWindowCandidate>)eventArgs.Candidates).Add(CreateCandidate((IntPtr)0x4002, 4002)));
    }

    [Fact]
    public void SelectWindow_WithCandidate_StoresCandidateHandle()
    {
        var candidate = CreateCandidate((IntPtr)0x5001, 5001);
        var eventArgs = new WindowSelectionRequestEventArgs(
            [candidate],
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5),
            startedProcessId: null,
            requestedExecutablePath: null,
            newlyDiscoveredCandidates: [candidate]);

        eventArgs.SelectWindow(candidate);

        Assert.Equal(candidate.WindowHandle, eventArgs.SelectedWindowHandle);
    }

    [Fact]
    public void SelectWindow_WithHandle_StoresHandle()
    {
        var eventArgs = new WindowSelectionRequestEventArgs(
            [],
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5),
            startedProcessId: null,
            requestedExecutablePath: null,
            newlyDiscoveredCandidates: []);

        eventArgs.SelectWindow((IntPtr)0x5002);

        Assert.Equal((IntPtr)0x5002, eventArgs.SelectedWindowHandle);
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
