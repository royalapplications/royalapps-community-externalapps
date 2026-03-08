namespace RoyalApps.Community.ExternalApps.WinForms.Hosting;

/// <summary>
/// Reflects the current state of an external application.
/// </summary>
public enum ApplicationState
{
    /// <summary>
    /// Indicates that the application is starting.
    /// </summary>
    Starting,
    /// <summary>
    /// Indicates that the application is running.
    /// </summary>
    Running,
    /// <summary>
    /// Indicates that the application is stopped.
    /// </summary>
    Stopped,
}
