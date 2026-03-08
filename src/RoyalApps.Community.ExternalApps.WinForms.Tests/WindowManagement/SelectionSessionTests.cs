using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class SelectionSessionTests
{
    [Fact]
    public async Task SelectWindowAsync_ReturnsSelectedCandidate_WhenHandlerSelectsCandidate()
    {
        var expectedCandidate = CreateCandidate((IntPtr)0x1001, 1001);
        var session = CreateSession([expectedCandidate]);
        var options = new ExternalAppSelectionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(50),
            PollInterval = TimeSpan.FromMilliseconds(1)
        };

        var result = await session.SelectWindowAsync(
            startedProcess: null,
            requestedExecutablePath: @"C:\Test\TestProcess.exe",
            baselineWindowHandles: null,
            options,
            eventArgs =>
            {
                Assert.Single(eventArgs.NewlyDiscoveredCandidates);
                eventArgs.SelectWindow(expectedCandidate);
            },
            CancellationToken.None);

        Assert.Equal(WindowSelectionOutcome.Selected, result.Outcome);
        Assert.Same(expectedCandidate, result.SelectedCandidate);
    }

    [Fact]
    public async Task SelectWindowAsync_ReturnsTimedOut_WhenNoCandidateIsSelected()
    {
        var session = CreateSession([CreateCandidate((IntPtr)0x2001, 2001)]);
        var options = new ExternalAppSelectionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            PollInterval = TimeSpan.FromMilliseconds(1)
        };
        var callCount = 0;

        var result = await session.SelectWindowAsync(
            startedProcess: null,
            requestedExecutablePath: @"C:\Test\TestProcess.exe",
            baselineWindowHandles: null,
            options,
            _ => Interlocked.Increment(ref callCount),
            CancellationToken.None);

        Assert.Equal(WindowSelectionOutcome.TimedOut, result.Outcome);
        Assert.Null(result.SelectedCandidate);
        Assert.True(callCount >= 1);
    }

    [Fact]
    public async Task SelectWindowAsync_ReturnsStartedProcessExited_WhenStartedProcessExitsBeforeSelection()
    {
        using var process = StartAndWaitForExit();
        var session = CreateSession([]);
        var options = new ExternalAppSelectionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(50),
            PollInterval = TimeSpan.FromMilliseconds(1)
        };

        var result = await session.SelectWindowAsync(
            process,
            requestedExecutablePath: "cmd.exe",
            baselineWindowHandles: null,
            options,
            raiseSelectionRequest: null,
            CancellationToken.None);

        Assert.Equal(WindowSelectionOutcome.StartedProcessExited, result.Outcome);
        Assert.Null(result.SelectedCandidate);
    }

    [Fact]
    public async Task SelectWindowAsync_IgnoresSelectedHandle_WhenHandleIsNotInCurrentCandidates()
    {
        var session = CreateSession([CreateCandidate((IntPtr)0x3001, 3001)]);
        var options = new ExternalAppSelectionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            PollInterval = TimeSpan.FromMilliseconds(1)
        };

        var result = await session.SelectWindowAsync(
            startedProcess: null,
            requestedExecutablePath: @"C:\Test\TestProcess.exe",
            baselineWindowHandles: null,
            options,
            eventArgs => eventArgs.SelectWindow((IntPtr)0x9999),
            CancellationToken.None);

        Assert.Equal(WindowSelectionOutcome.TimedOut, result.Outcome);
    }

    [Fact]
    public async Task SelectWindowAsync_PassesRequestedExecutablePathToSelectionCallback()
    {
        var expectedPath = @"C:\Windows\System32\notepad.exe";
        var session = CreateSession([CreateCandidate((IntPtr)0x3002, 3002)]);
        var options = new ExternalAppSelectionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            PollInterval = TimeSpan.FromMilliseconds(1)
        };
        string? observedPath = null;

        var result = await session.SelectWindowAsync(
            startedProcess: null,
            requestedExecutablePath: expectedPath,
            baselineWindowHandles: null,
            options,
            eventArgs => observedPath = eventArgs.RequestedExecutablePath,
            CancellationToken.None);

        Assert.Equal(WindowSelectionOutcome.TimedOut, result.Outcome);
        Assert.Equal(expectedPath, observedPath);
    }

    [Fact]
    public async Task SelectWindowAsync_ReportsCandidatesAsNewOnlyOnFirstObservation()
    {
        var candidate = CreateCandidate((IntPtr)0x3003, 3003);
        var session = new SelectionSession(
            new SequencedWindowCatalog(
                [candidate],
                [candidate]),
            NullLogger<SelectionSession>.Instance);
        var options = new ExternalAppSelectionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            PollInterval = TimeSpan.FromMilliseconds(1)
        };
        var callCount = 0;
        var newCounts = new List<int>();

        var result = await session.SelectWindowAsync(
            startedProcess: null,
            requestedExecutablePath: @"C:\Test\TestProcess.exe",
            baselineWindowHandles: null,
            options,
            eventArgs =>
            {
                callCount++;
                newCounts.Add(eventArgs.NewlyDiscoveredCandidates.Count);
            },
            CancellationToken.None);

        Assert.Equal(WindowSelectionOutcome.TimedOut, result.Outcome);
        Assert.True(callCount >= 2);
        Assert.Equal(1, newCounts[0]);
        Assert.Contains(0, newCounts);
    }

    [Fact]
    public async Task SelectWindowAsync_TreatsBaselineWindowsAsAlreadyKnown()
    {
        var existingCandidate = CreateCandidate((IntPtr)0x4001, 4001);
        var newCandidate = CreateCandidate((IntPtr)0x4002, 4002);
        var session = new SelectionSession(
            new StubWindowCatalog([existingCandidate, newCandidate]),
            NullLogger<SelectionSession>.Instance);
        var options = new ExternalAppSelectionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            PollInterval = TimeSpan.FromMilliseconds(1)
        };
        IReadOnlyList<ExternalWindowCandidate>? observedNewCandidates = null;

        var result = await session.SelectWindowAsync(
            startedProcess: null,
            requestedExecutablePath: @"C:\Test\TestProcess.exe",
            baselineWindowHandles: new HashSet<nint> { existingCandidate.WindowHandle },
            options,
            eventArgs =>
            {
                observedNewCandidates = eventArgs.NewlyDiscoveredCandidates;
                eventArgs.SelectWindow(newCandidate);
            },
            CancellationToken.None);

        Assert.Equal(WindowSelectionOutcome.Selected, result.Outcome);
        Assert.NotNull(observedNewCandidates);
        Assert.Single(observedNewCandidates!);
        Assert.Equal(newCandidate.WindowHandle, observedNewCandidates[0].WindowHandle);
    }

    private static SelectionSession CreateSession(IReadOnlyList<ExternalWindowCandidate> candidates)
    {
        return new SelectionSession(new StubWindowCatalog(candidates), NullLogger<SelectionSession>.Instance);
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

    private static Process StartAndWaitForExit()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c exit 0",
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();
        return process;
    }

    private sealed class StubWindowCatalog : IWindowCatalog
    {
        private readonly IReadOnlyList<ExternalWindowCandidate> _candidates;

        public StubWindowCatalog(IReadOnlyList<ExternalWindowCandidate> candidates)
        {
            _candidates = candidates;
        }

        public IReadOnlyList<ExternalWindowCandidate> GetAvailableWindows()
        {
            return _candidates;
        }
    }

    private sealed class SequencedWindowCatalog : IWindowCatalog
    {
        private readonly Queue<IReadOnlyList<ExternalWindowCandidate>> _batches;
        private IReadOnlyList<ExternalWindowCandidate> _lastBatch = [];

        public SequencedWindowCatalog(params IReadOnlyList<ExternalWindowCandidate>[] batches)
        {
            _batches = new Queue<IReadOnlyList<ExternalWindowCandidate>>(batches);
        }

        public IReadOnlyList<ExternalWindowCandidate> GetAvailableWindows()
        {
            if (_batches.Count > 0)
                _lastBatch = _batches.Dequeue();

            return _lastBatch;
        }
    }
}
