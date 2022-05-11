namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// 
/// </summary>
public class ExternalAppConfiguration
{
    /// <summary>
    /// Gets or sets the command (executable) of the external application.
    /// </summary>
    public string? Command { get; set; }
    
    /// <summary>
    /// Gets or sets the arguments that will be passed to the executable.
    /// </summary>
    public string? Arguments { get; set; }
    
    /// <summary>
    /// Gets or sets the Working Directory the command will be started from.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether not to use credentials to start the process.
    /// </summary>
    public bool RunElevated { get; set; }
    
    /// <summary>
    /// Gets or sets whether not to use credentials to start the process.
    /// </summary>
    public bool UseCredentials { get; set; }
    
    /// <summary>
    /// Gets or sets the user name to start the application with (works only in combination with UseCredentials).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the domain to start the application with (works only in combination with UseCredentials).
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the password to start the application with (works only in combination with UseCredentials).
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// Gets or sets whether not to load the user profile (works only in combination with UseCredentials).
    /// </summary>
    public bool LoadUserProfile { get; set; }
    
    /// <summary>
    /// If set, a matching command line is looked up first.
    /// </summary>
    public string? CommandLineMatchString { get; set; }

    /// <summary>
    /// Gets or sets the minimum time (in seconds) the control will wait to grab the window handle of the external application after launching it.
    /// Default: 0
    /// </summary>
    public int MinWaitTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum time (in seconds) the control will wait to grab the window handle of the external application after launching it.
    /// Default: 10
    /// </summary>
    public int MaxWaitTime { get; set; } = 10;
    
    /// <summary>
    /// If set, another process name is looked up first
    /// </summary>
    public string? ProcessNameToTrack { get; set; }

    /// <summary>
    /// Gets or sets whether the external process will be started hidden (CreateNoWindow = true).
    /// Default: true
    /// </summary>
    public bool StartHidden { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the external process will not be embedded and kept as external window.
    /// </summary>
    public bool StartExternal { get; set; }
    
    /// <summary>
    /// Gets or sets whether to use an existing process or start a new one.
    /// </summary>
    public bool UseExistingProcess { get; set; }
    
    /// <summary>
    /// Gets or sets the window title to match.
    /// </summary>
    public string WindowTitleMatch { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets how many window title matches should be skipped before embedding.
    /// </summary>
    public int WindowTitleMatchSkip { get; set; }
    
    /// <summary>
    /// Gets or sets whether or not to kill the process when the app is closed.
    /// </summary>
    public bool KillOnClose { get; set; }
}