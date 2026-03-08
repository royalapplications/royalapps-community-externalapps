using System;

namespace RoyalApps.Community.ExternalApps.WinForms.Events;

/// <summary>
/// Event arguments for the ApplicationClosed event.
/// </summary>
public class ApplicationClosedEventArgs : EventArgs
{
    /// <summary>
    /// True, if the user has closed the application using the CloseApplication() method, otherwise false.
    /// </summary>
    public bool UserInitiated { get; set; }

    /// <summary>
    /// True, if the process exited.
    /// </summary>
    public bool ProcessExited { get; set; }

    /// <summary>
    /// The exception which may have caused the application to close.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationClosedEventArgs"/> class.
    /// </summary>
    public ApplicationClosedEventArgs()
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationClosedEventArgs"/> class and sets the <see cref="Exception"/> property.
    /// </summary>
    /// <param name="exception">The exception that caused the session to close.</param>
    public ApplicationClosedEventArgs(Exception exception)
    {
        Exception = exception;
    }
}
