using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Win32.Foundation;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class ExternalAppSessionTests
{
    [Fact]
    public async Task StartAsync_SelectedWindow_StartsInExternalAttachmentStateAndTracksWindow()
    {
        var windowHandle = (IntPtr)0x7101;
        var candidate = CreateCandidate(windowHandle, Environment.ProcessId);
        var session = CreateSession(
            ProcessLaunchResult.ExistingProcess(),
            WindowSelectionResult.Selected(candidate));

        try
        {
            await session.StartAsync(null, CancellationToken.None);

            Assert.Equal(ApplicationState.Running, session.ApplicationState);
            Assert.Equal(AttachmentState.External, session.AttachmentState);
            Assert.True(session.IsExternal);
            Assert.False(session.IsEmbedded);
            Assert.True(session.HasWindow);
            Assert.True(ExternalApps.IsTrackedWindow(new HWND(windowHandle)));
        }
        finally
        {
            session.Dispose();
        }

        Assert.False(ExternalApps.IsTrackedWindow(new HWND(windowHandle)));
    }

    [Fact]
    public async Task StartAsync_TimedOutSelection_RemainsUnattached()
    {
        var session = CreateSession(
            ProcessLaunchResult.Started(Process.GetCurrentProcess()),
            WindowSelectionResult.TimedOut());

        await session.StartAsync(null, CancellationToken.None);

        Assert.Equal(ApplicationState.Running, session.ApplicationState);
        Assert.Equal(AttachmentState.None, session.AttachmentState);
        Assert.False(session.IsExternal);
        Assert.False(session.HasWindow);
        Assert.False(session.IsEmbedded);

        session.Dispose();
    }

    [Fact]
    public async Task EmbedAsync_ThenDetachApplication_UpdatesAttachmentState()
    {
        var candidate = CreateCandidate((IntPtr)0x7102, Environment.ProcessId);
        var embeddingController = new StubEmbeddingController();
        var session = CreateSession(
            ProcessLaunchResult.ExistingProcess(),
            WindowSelectionResult.Selected(candidate),
            embeddingController);

        try
        {
            await session.StartAsync(null, CancellationToken.None);
            await session.EmbedAsync(null!, CancellationToken.None);

            Assert.Equal(AttachmentState.Embedded, session.AttachmentState);
            Assert.True(session.IsEmbedded);
            Assert.True(embeddingController.EmbedCalled);

            session.DetachApplication();

            Assert.Equal(AttachmentState.Detached, session.AttachmentState);
            Assert.False(session.IsEmbedded);
            Assert.True(embeddingController.DetachCalled);
        }
        finally
        {
            session.Dispose();
        }
    }

    [Fact]
    public async Task MarkAsExternal_ChangesDetachedWindowBackToExternalState()
    {
        var candidate = CreateCandidate((IntPtr)0x7103, Environment.ProcessId);
        var session = CreateSession(
            ProcessLaunchResult.ExistingProcess(),
            WindowSelectionResult.Selected(candidate),
            new StubEmbeddingController());

        try
        {
            await session.StartAsync(null, CancellationToken.None);
            await session.EmbedAsync(null!, CancellationToken.None);
            session.DetachApplication();

            session.MarkAsExternal();

            Assert.Equal(AttachmentState.External, session.AttachmentState);
            Assert.True(session.IsExternal);
        }
        finally
        {
            session.Dispose();
        }
    }

    private static ExternalAppSession CreateSession(
        ProcessLaunchResult launchResult,
        WindowSelectionResult selectionResult,
        IEmbeddingController? embeddingController = null)
    {
        return new ExternalAppSession(
            new ExternalAppOptions
            {
                Launch = { Executable = "cmd.exe" }
            },
            NullLoggerFactory.Instance,
            new StubProcessLauncher(launchResult),
            new StubSelectionSession(selectionResult),
            embeddingController ?? new StubEmbeddingController());
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

    private sealed class StubProcessLauncher : IProcessLauncher
    {
        private readonly ProcessLaunchResult _result;

        public StubProcessLauncher(ProcessLaunchResult result)
        {
            _result = result;
        }

        public Task<ProcessLaunchResult> StartAsync(ExternalAppOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class StubSelectionSession : ISelectionSession
    {
        private readonly WindowSelectionResult _result;

        public StubSelectionSession(WindowSelectionResult result)
        {
            _result = result;
        }

        public Task<WindowSelectionResult> SelectWindowAsync(
            Process? startedProcess,
            string? requestedExecutablePath,
            IReadOnlySet<nint>? baselineWindowHandles,
            ExternalAppSelectionOptions options,
            Action<WindowSelectionRequestEventArgs>? raiseSelectionRequest,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }

        public IReadOnlySet<nint> CaptureKnownWindowHandles()
        {
            return new HashSet<nint>();
        }
    }

    private sealed class StubEmbeddingController : IEmbeddingController
    {
        public bool IsEmbedded { get; private set; }

        public bool EmbedCalled { get; private set; }

        public bool DetachCalled { get; private set; }

        public Task EmbedAsync(ExternalAppHost ownerControl, HWND windowHandle, ExternalAppEmbeddingOptions options, CancellationToken cancellationToken)
        {
            EmbedCalled = true;
            IsEmbedded = true;
            return Task.CompletedTask;
        }

        public void Detach(HWND windowHandle)
        {
            DetachCalled = true;
            IsEmbedded = false;
        }

        public void Focus(HWND windowHandle)
        {
        }

        public void SetWindowPosition(HWND windowHandle, Rectangle bounds, ExternalAppEmbeddingOptions options)
        {
        }

        public void Dispose()
        {
        }
    }
}
