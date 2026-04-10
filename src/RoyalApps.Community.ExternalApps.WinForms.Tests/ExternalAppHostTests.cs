using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using System.Windows.Forms;
using Windows.Win32.Foundation;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests;

public sealed class ExternalAppHostTests
{
    [Fact]
    public void Properties_ReflectCoordinatorState()
    {
        using var process = Process.GetCurrentProcess();
        using var coordinator = new StubSessionCoordinator
        {
            Options = new ExternalAppOptions { Launch = { Executable = "cmd.exe" } },
            Process = process,
            WindowHandle = new HWND((IntPtr)0x8101),
            AttachmentState = AttachmentState.Detached,
            IsEmbedded = false
        };
        using var host = CreateHost(coordinator);

        Assert.Equal((IntPtr)0x8101, host.EmbeddedWindowHandle);
        Assert.Equal(coordinator.Options, host.Options);
        Assert.Equal(AttachmentState.Detached, host.AttachmentState);
        Assert.False(host.IsEmbedded);
        Assert.Same(process, host.Process);
    }

    [Fact]
    public async Task Start_RaisesWindowSelectionRequested_AndApplicationStarted()
    {
        var selectionRaised = new TaskCompletionSource<WindowSelectionRequestEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedRaised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var coordinator = new StubSessionCoordinator();
        coordinator.OnStartAsync = (_, raiseSelectionRequest, _) =>
        {
            raiseSelectionRequest?.Invoke(new WindowSelectionRequestEventArgs(
                [],
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                1234,
                "cmd.exe",
                []));

            coordinator.IsRunning = true;
            return Task.CompletedTask;
        };

        await RunHostedAsync(coordinator, async host =>
        {
            host.WindowSelectionRequested += (_, e) => selectionRaised.TrySetResult(e);
            host.ApplicationStarted += (_, _) => startedRaised.TrySetResult();

            host.Start(new ExternalAppOptions { Launch = { Executable = "cmd.exe" } });

            await selectionRaised.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await startedRaised.Task.WaitAsync(TimeSpan.FromSeconds(2));
        });
    }

    [Fact]
    public async Task Start_WhenCoordinatorThrows_RaisesApplicationClosed()
    {
        var closedRaised = new TaskCompletionSource<ApplicationClosedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("boom");
        using var coordinator = new StubSessionCoordinator
        {
            OnStartAsync = (_, _, _) => Task.FromException(failure)
        };

        await RunHostedAsync(coordinator, async host =>
        {
            host.ApplicationClosed += (_, e) => closedRaised.TrySetResult(e);

            host.Start(new ExternalAppOptions { Launch = { Executable = "cmd.exe" } });

            var eventArgs = await closedRaised.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Same(failure, eventArgs.Exception);
        });
    }

    [Fact]
    public async Task Start_WhenEmbeddingFails_MarksSessionExternal_AndStillRaisesApplicationStarted()
    {
        var startedRaised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var coordinator = new StubSessionCoordinator
        {
            WindowHandle = new HWND((IntPtr)0x8102),
            HasWindow = true,
            IsRunning = true,
            OnStartAsync = (_, _, _) => Task.CompletedTask,
            OnEmbedAsync = (_, _) => Task.FromException(new EmbeddingFailedException("embed failed"))
        };

        await RunHostedAsync(coordinator, async host =>
        {
            host.ApplicationStarted += (_, _) => startedRaised.TrySetResult();

            host.Start(new ExternalAppOptions
            {
                Launch = { Executable = "cmd.exe" },
                Embedding = { StartEmbedded = true }
            });

            await startedRaised.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(1, coordinator.MarkAsExternalCallCount);
            Assert.Equal(AttachmentState.External, coordinator.AttachmentState);
            Assert.False(host.IsEmbedded);
        });
    }

    [Fact]
    public async Task Start_WhenWindowIsSelectedAndStartEmbedded_IsStillAttemptsEmbedding()
    {
        var startedRaised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var coordinator = new StubSessionCoordinator
        {
            WindowHandle = new HWND((IntPtr)0x8103),
            HasWindow = true,
            IsRunning = true,
            AttachmentState = AttachmentState.External,
            OnStartAsync = (_, _, _) => Task.CompletedTask
        };

        await RunHostedAsync(coordinator, async host =>
        {
            host.ApplicationStarted += (_, _) => startedRaised.TrySetResult();

            host.Start(new ExternalAppOptions
            {
                Launch = { Executable = "cmd.exe" },
                Embedding = { StartEmbedded = true }
            });

            await startedRaised.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(1, coordinator.EmbedCallCount);
            Assert.Equal(AttachmentState.Embedded, coordinator.AttachmentState);
            Assert.True(host.IsEmbedded);
        });
    }

    [Fact]
    public void Start_WithoutCreatedHandle_ThrowsInvalidOperationException()
    {
        using var coordinator = new StubSessionCoordinator();
        using var host = CreateHost(coordinator);

        var exception = Assert.Throws<InvalidOperationException>(
            () => host.Start(new ExternalAppOptions { Launch = { Executable = "cmd.exe" } }));

        Assert.Contains("created window handle", exception.Message, StringComparison.Ordinal);
    }

    private static ExternalAppHost CreateHost(StubSessionCoordinator coordinator)
    {
        return new ExternalAppHost(coordinator, NullLoggerFactory.Instance);
    }

    private static Task RunHostedAsync(
        StubSessionCoordinator coordinator,
        Func<ExternalAppHost, Task> testBody)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            using var form = new Form();
            using var host = CreateHost(coordinator);
            host.Dock = DockStyle.Fill;
            form.Controls.Add(host);

            form.Shown += async (_, _) =>
            {
                try
                {
                    await testBody(host);
                    completion.TrySetResult();
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
                finally
                {
                    form.Close();
                }
            };

            try
            {
                Application.Run(form);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        return completion.Task;
    }

    private sealed class StubSessionCoordinator : IExternalAppHostSessionCoordinator
    {
        public event EventHandler<ApplicationClosedEventArgs>? SessionClosed;

        public ExternalAppOptions? Options { get; set; }

        public Process? Process { get; set; }

        public HWND WindowHandle { get; set; }

        public bool HasWindow { get; set; }

        public AttachmentState AttachmentState { get; set; }

        public bool IsEmbedded { get; set; }

        public bool IsRunning { get; set; }

        public bool IsExternal => AttachmentState == AttachmentState.External;

        public int MarkAsExternalCallCount { get; private set; }

        public int EmbedCallCount { get; private set; }

        public Func<ExternalAppOptions, Action<WindowSelectionRequestEventArgs>?, CancellationToken, Task>? OnStartAsync { get; set; }

        public Func<ExternalAppHost, CancellationToken, Task>? OnEmbedAsync { get; set; }

        public Task StartAsync(
            ExternalAppOptions options,
            Action<WindowSelectionRequestEventArgs>? raiseSelectionRequest,
            CancellationToken cancellationToken)
        {
            Options = options;
            return OnStartAsync?.Invoke(options, raiseSelectionRequest, cancellationToken) ?? Task.CompletedTask;
        }

        public Task EmbedAsync(ExternalAppHost ownerControl, CancellationToken cancellationToken)
        {
            EmbedCallCount++;
            IsEmbedded = true;
            AttachmentState = AttachmentState.Embedded;
            return OnEmbedAsync?.Invoke(ownerControl, cancellationToken) ?? Task.CompletedTask;
        }

        public void CloseApplication()
        {
        }

        public void DetachApplication()
        {
            IsEmbedded = false;
            AttachmentState = AttachmentState.Detached;
        }

        public void FocusApplication()
        {
        }

        public void MarkAsExternal()
        {
            MarkAsExternalCallCount++;
            IsEmbedded = false;
            AttachmentState = AttachmentState.External;
        }

        public void SetWindowPosition(Rectangle rectangle)
        {
        }

        public string GetWindowTitle()
        {
            return string.Empty;
        }

        public void Dispose()
        {
        }

        public void RaiseSessionClosed(ApplicationClosedEventArgs eventArgs)
        {
            SessionClosed?.Invoke(this, eventArgs);
        }
    }
}
