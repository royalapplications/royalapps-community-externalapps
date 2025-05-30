using System;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

/// <summary>
/// The event args contain the new window caption
/// </summary>
public class WindowCaptionEventArgs : EventArgs
{
    /// <summary>
    /// Window caption
    /// </summary>
    public string Caption { get; }

    /// <summary>
    /// Creates a new instance of the WindowCaptionEventArgs with the new caption
    /// </summary>
    /// <param name="caption"></param>
    public WindowCaptionEventArgs(string caption)
    {
        Caption = caption;
    }
}
