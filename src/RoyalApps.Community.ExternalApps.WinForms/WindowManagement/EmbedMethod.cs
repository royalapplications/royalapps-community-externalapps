namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

/// <summary>
/// How to embed the external application window into the ExternalAppHost.
/// </summary>
public enum EmbedMethod
{
    /// <summary>
    /// The whole window is embedded including the main menu (if available).
    /// The limitation of this method is that the ALT-TAB order may be incorrect.
    /// </summary>
    Window,
    /// <summary>
    /// Only the client area of the external app window is embedded (without the main menu).
    /// The limitation of this method is that some applications may look like they are not focused/active.
    /// </summary>
    Control
}
