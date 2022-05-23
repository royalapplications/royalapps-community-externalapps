using System;

namespace RoyalApps.Community.ExternalApps.WinForms;

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
    /// Creates instance.
    /// </summary>
    public ApplicationClosedEventArgs()
    {
        
    }
    
    /// <summary>
    /// Creates an instance and sets the Exception property.
    /// </summary>
    /// <param name="exception"></param>
    public ApplicationClosedEventArgs(Exception exception)
    {
        Exception = exception;
    }
}