using System.ComponentModel;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// Setup and initialize external apps hosting.
/// </summary>
public static class ExternalApps
{
    private static readonly ProcessJobTracker ProcessJobTracker = new("ExternalApps");

    /// <summary>
    /// Must be called when the application host starts. 
    /// </summary>
    public static void Initialize()
    {
        ExternalAppsNative.InitShl();
    }

    /// <summary>
    /// Must be called when before the application host is closed.
    /// </summary>
    public static void Cleanup()
    {
        ExternalAppsNative.DoneShl();
        ProcessJobTracker.Dispose();
    }

    internal static void EmbedWindow(HWND parentWindowHandle, HWND childWindowHandle, Process? process)
    {
        if (!PInvoke.GetWindowRect(parentWindowHandle, out var parentRect))
            throw new Win32Exception();
            
        ExternalAppsNative.CreateShlWnd(parentWindowHandle, childWindowHandle, parentRect.right - parentRect.left, parentRect.bottom - parentRect.top);

        if (process != null) 
            ProcessJobTracker.AddProcess(process);
    }
}