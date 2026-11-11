using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RoyalApps.Community.ExternalApps.WinForms.WindowManagement;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests;

public class ExternalAppTests
{
    [Fact]
    public async Task MissingProcessNameDoesNotCaptureAnUnrelatedProcess()
    {
        using var app = CreateApp(new ExternalAppConfiguration
        {
            UseExistingProcess = true,
            ProcessNameToTrack = $"missing-{Guid.NewGuid():N}.exe",
            MaxWaitTime = 0
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync(CancellationToken.None));

        Assert.Null(app.Process);
        Assert.Equal(ApplicationState.Stopped, app.ApplicationState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanFindExistingProcessByCommandLine(bool includeProcessName)
    {
        using var searcher = new ManagementObjectSearcher(
            $"SELECT Name, CommandLine FROM Win32_Process WHERE ProcessId={Environment.ProcessId}");
        using var results = searcher.Get();
        string? processName = null;
        string? commandLine = null;
        foreach (ManagementObject result in results)
        {
            using (result)
            {
                processName = (string)result["Name"];
                commandLine = (string)result["CommandLine"];
            }
        }
        Assert.False(string.IsNullOrWhiteSpace(commandLine));

        using var app = CreateApp(new ExternalAppConfiguration
        {
            UseExistingProcess = true,
            ProcessNameToTrack = includeProcessName ? processName : null,
            CommandLineMatchString = commandLine!.ToUpperInvariant(),
            MaxWaitTime = 0
        });

        await app.StartAsync(CancellationToken.None);

        Assert.Equal(Environment.ProcessId, app.Process?.Id);
        Assert.Equal(ApplicationState.Running, app.ApplicationState);
    }

    [Fact]
    public async Task CancellationIsPropagatedAndAllowsRetry()
    {
        using var app = CreateApp(new ExternalAppConfiguration { UseExistingProcess = true });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => app.StartAsync(cancellation.Token));
        Assert.Equal(ApplicationState.Stopped, app.ApplicationState);

        // A second attempt reaches process discovery rather than failing the state guard.
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync(CancellationToken.None));
        Assert.Equal("Failed to start or capture process.", error.Message);
        Assert.Equal(ApplicationState.Stopped, app.ApplicationState);
    }

    [Fact]
    public async Task CancellationWhileWaitingForProcessIsPropagated()
    {
        using var app = CreateApp(new ExternalAppConfiguration
        {
            UseExistingProcess = true,
            ProcessNameToTrack = "missing.exe",
            MinWaitTime = 10
        });
        using var cancellation = new CancellationTokenSource();
        var start = app.StartAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Equal(ApplicationState.Stopped, app.ApplicationState);
    }

    private static ExternalApp CreateApp(ExternalAppConfiguration configuration) =>
        new(configuration, NullLoggerFactory.Instance);
}
