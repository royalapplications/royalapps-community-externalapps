namespace RoyalApps.Community.ExternalApps.WinForms.Embedding;

/// <summary>
/// Describes how the selected external window is currently attached to the host.
/// </summary>
public enum AttachmentState
{
    /// <summary>
    /// Indicates that no window is currently attached to the session.
    /// </summary>
    None,

    /// <summary>
    /// Indicates that a window is attached but currently left external instead of being reparented into the host.
    /// </summary>
    External,

    /// <summary>
    /// Indicates that a window is attached to the session but has been detached from the host control.
    /// </summary>
    Detached,

    /// <summary>
    /// Indicates that a window is currently embedded in the host control.
    /// </summary>
    Embedded,
}
