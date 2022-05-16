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

    internal static HWND EmbedWindow(HWND parentWindowHandle, HWND childWindowHandle, Process? process)
    {
        if (!PInvoke.GetClientRect(parentWindowHandle, out var parentWindowClientRect))
            throw new Win32Exception();
            
        var containerHandle = ExternalAppsNative.CreateShlWnd(
            parentWindowHandle, 
            childWindowHandle, 
            parentWindowClientRect.right - parentWindowClientRect.left, 
            parentWindowClientRect.bottom - parentWindowClientRect.top);

        if (process != null) 
            ProcessJobTracker.AddProcess(process);

        return new HWND(containerHandle);
    }

    internal static HWND DetachWindow(HWND windowHandle)
    {
        var newWindowHandle = PInvoke.SendMessage(windowHandle, PInvoke.WM_PARENTNOTIFY, new WPARAM(PInvoke.WM_NCDESTROY), 0);
        PInvoke.DestroyWindow(windowHandle);
        return new HWND(newWindowHandle);

    }
    
}