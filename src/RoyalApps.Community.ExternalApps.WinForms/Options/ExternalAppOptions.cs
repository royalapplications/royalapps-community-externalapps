using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using System;
using System.Collections.Generic;

namespace RoyalApps.Community.ExternalApps.WinForms.Options;

/// <summary>
/// Defines the runtime options for launching, selecting and embedding an external application.
/// </summary>
public sealed class ExternalAppOptions
{
    /// <summary>
    /// Gets the launch options.
    /// </summary>
    public ExternalAppLaunchOptions Launch { get; init; } = new();

    /// <summary>
    /// Gets the embedding options.
    /// </summary>
    public ExternalAppEmbeddingOptions Embedding { get; init; } = new();

    /// <summary>
    /// Gets the selection options.
    /// </summary>
    public ExternalAppSelectionOptions Selection { get; init; } = new();
}

/// <summary>
/// Defines how the external process is launched or attached.
/// </summary>
public sealed class ExternalAppLaunchOptions
{
    /// <summary>
    /// Gets or sets the full path to the executable that should be started.
    /// </summary>
    public string? Executable { get; set; }

    /// <summary>
    /// Gets or sets the command-line arguments passed to the executable.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the working directory used when the process is started.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the process should be launched elevated.
    /// </summary>
    public bool RunElevated { get; set; }

    /// <summary>
    /// Gets the credential options used when starting the process under alternate credentials.
    /// </summary>
    public ExternalAppCredentialOptions Credentials { get; init; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether an already running process should be reused instead of launching a new one.
    /// </summary>
    public bool UseExistingProcess { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether newly started processes should initially be hidden while selection runs.
    /// </summary>
    public bool StartHidden { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the tracked process should be terminated if graceful close does not succeed.
    /// </summary>
    public bool KillOnClose { get; set; }

    /// <summary>
    /// Gets the environment variables applied to the started process when direct process creation is used.
    /// </summary>
    /// <remarks>
    /// Custom environment variables are not supported for elevated launches because elevation requires shell execution.
    /// </remarks>
    public IDictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Defines credential options used for process launch.
/// </summary>
public sealed class ExternalAppCredentialOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether alternate credentials should be used for process launch.
    /// </summary>
    public bool UseCredentials { get; set; }

    /// <summary>
    /// Gets or sets the user name used for alternate-credential process launch.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the domain used for alternate-credential process launch.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the password used for alternate-credential process launch.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user profile should be loaded when alternate credentials are used.
    /// </summary>
    public bool LoadUserProfile { get; set; }
}

/// <summary>
/// Defines how a resolved window is embedded.
/// </summary>
public sealed class ExternalAppEmbeddingOptions
{
    /// <summary>
    /// Gets or sets the embedding mode used for the selected window.
    /// </summary>
    public EmbedMethod Mode { get; set; } = EmbedMethod.Control;

    /// <summary>
    /// Gets or sets a value indicating whether the selected window should be embedded immediately after startup.
    /// </summary>
    public bool StartEmbedded { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the embedded window's non-client area, such as the title bar and frame, should be accounted for when sizing it to the host bounds.
    /// </summary>
    /// <remarks>
    /// When enabled, the library expands the target rectangle so the embedded window's client area fills the requested bounds.
    /// When disabled, the requested bounds are applied directly to the embedded window.
    /// </remarks>
    public bool IncludeWindowChromeDimensions { get; set; } = true;
}

/// <summary>
/// Defines discovery and selection timing.
/// </summary>
public sealed class ExternalAppSelectionOptions
{
    /// <summary>
    /// Gets or sets the maximum time allowed for candidate discovery and selection before the application is left running externally.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the interval between candidate discovery polls while the host is deciding which window to embed.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(250);
}
