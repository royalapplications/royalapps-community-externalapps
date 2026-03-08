using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.Extensions;

namespace RoyalApps.Community.ExternalApps.WinForms.Launching;

internal sealed class ProcessLauncher : IProcessLauncher
{
    private readonly ILogger<ProcessLauncher> _logger;
    private readonly EnvironmentVariableExpander _environmentVariableExpander;
    private readonly ExecutableResolver _executableResolver;

    public ProcessLauncher(
        ILogger<ProcessLauncher> logger,
        ExecutableResolver? executableResolver = null,
        EnvironmentVariableExpander? environmentVariableExpander = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executableResolver = executableResolver ?? new ExecutableResolver();
        _environmentVariableExpander = environmentVariableExpander ?? new EnvironmentVariableExpander();
    }

    public async Task<ProcessLaunchResult> StartAsync(
        ExternalAppOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var launchOptions = options.Launch;
        var embeddingOptions = options.Embedding;

        if (launchOptions.UseExistingProcess)
            return ProcessLaunchResult.ExistingProcess();

        if (string.IsNullOrWhiteSpace(launchOptions.Executable))
            throw new InvalidOperationException("No executable has been configured.");

        if (launchOptions is { RunElevated: true, EnvironmentVariables.Count: > 0 })
            throw new InvalidOperationException("Custom environment variables are not supported when launching elevated processes.");

        var expandedExecutable = _environmentVariableExpander.Expand(launchOptions.Executable, launchOptions.EnvironmentVariables);
        var expandedWorkingDirectory = _environmentVariableExpander.Expand(launchOptions.WorkingDirectory, launchOptions.EnvironmentVariables);

        var requiresDirectStart =
            launchOptions.Credentials.UseCredentials ||
            launchOptions.EnvironmentVariables.Count > 0;
        var resolvedExecutable = requiresDirectStart
            ? _executableResolver.Resolve(expandedExecutable!, launchOptions.EnvironmentVariables)
            : expandedExecutable!;

        var processStartInfo = new ProcessStartInfo(
            resolvedExecutable,
            launchOptions.Arguments ?? string.Empty)
        {
            WindowStyle = launchOptions.StartHidden && embeddingOptions.StartEmbedded
                ? ProcessWindowStyle.Minimized
                : ProcessWindowStyle.Normal,
            CreateNoWindow = launchOptions.StartHidden && embeddingOptions.StartEmbedded,
            UseShellExecute = !requiresDirectStart,
            WorkingDirectory = expandedWorkingDirectory ?? ".",
        };

        if (launchOptions.RunElevated)
        {
            processStartInfo.Verb = "runas";
        }

        if (requiresDirectStart)
        {
            processStartInfo.LoadUserProfile = launchOptions.Credentials.LoadUserProfile;

            foreach (var pair in launchOptions.EnvironmentVariables)
                processStartInfo.Environment[pair.Key] = pair.Value;
        }

        if (launchOptions.Credentials.UseCredentials)
        {
            if (!string.IsNullOrWhiteSpace(launchOptions.Credentials.Username))
                processStartInfo.UserName = launchOptions.Credentials.Username;
            if (!string.IsNullOrWhiteSpace(launchOptions.Credentials.Domain))
                processStartInfo.Domain = launchOptions.Credentials.Domain;
            if (!string.IsNullOrWhiteSpace(launchOptions.Credentials.Password))
                processStartInfo.Password = SecureStringExtensions.ConvertToSecureString(launchOptions.Credentials.Password);
        }

        var process = Process.Start(processStartInfo);
        if (process == null)
            throw new InvalidOperationException($"Failed to start process '{launchOptions.Executable}'.");

        try
        {
            process.WaitForInputIdle();
        }
        catch
        {
            // Some processes don't support WaitForInputIdle.
        }

        var waitAttempts = 0;
        while (!process.HasExited && waitAttempts < 20)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero || !string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Process refresh failed while waiting for the main window");
            }

            waitAttempts++;
            await Task.Delay(100, cancellationToken);
        }

        if (process.HasExited)
        {
            _logger.LogInformation(
                "The started process '{Executable}' exited before exposing a usable window. Continuing with window discovery because the process may have been a launcher stub.",
                resolvedExecutable);
            return ProcessLaunchResult.LauncherExitedBeforeWindowDiscovery();
        }

        return ProcessLaunchResult.Started(process);
    }
}
