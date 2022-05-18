using System;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

/// <summary>
/// The event args used to get the window handle in case no window with the matching criteria has been found.
/// </summary>
public class QueryWindowEventArgs : EventArgs
{
    private int _windowHandle;

    internal int WindowHandle => _windowHandle;

    /// <summary>
    /// Sets the window handle to embed.
    /// </summary>
    /// <param name="windowHandle">Window handle</param>
    public void SetWindowHandle(int windowHandle)
    {
        _windowHandle = windowHandle;
    }
}