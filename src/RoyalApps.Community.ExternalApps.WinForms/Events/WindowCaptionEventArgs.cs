using System;

namespace RoyalApps.Community.ExternalApps.WinForms.Events;

/// <summary>
/// Provides the updated window caption for the tracked application.
/// </summary>
public class WindowCaptionEventArgs : EventArgs
{
    /// <summary>
    /// Gets the updated window caption.
    /// </summary>
    public string Caption { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowCaptionEventArgs"/> class.
    /// </summary>
    /// <param name="caption">The updated window caption.</param>
    public WindowCaptionEventArgs(string caption)
    {
        Caption = caption;
    }
}
