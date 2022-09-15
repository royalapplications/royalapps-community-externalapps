using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// Setup and initialize external apps hosting.
/// </summary>
public static class ExternalApps
{
    private static readonly ProcessJobTracker ProcessJobTracker = new("ExternalApps");
    private static bool _isInitialized;
    
    /// <summary>
    /// Must be called when the application host starts. 
    /// </summary>
    public static void Initialize(ILogger? logger = null)
    {
        try
        {
            ExternalAppsNative.InitShl();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "ExternalApps initialization failed. Window embedding may not work.");
        }
    }

    /// <summary>
    /// Must be called when before the application host is closed.
    /// </summary>
    public static void Cleanup(ILogger? logger = null)
    {
        try
        {
            ExternalAppsNative.DoneShl();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "ExternalApps cleanup failed.");
        }
        ProcessJobTracker.Dispose();
    }

    internal static HWND EmbedWindow(HWND parentWindowHandle, HWND childWindowHandle, Process? process, ILogger logger)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Cannot call EmbedWindow without calling 'ExternalApps.Initialize()' first.");
        
        if (!PInvoke.GetClientRect(parentWindowHandle, out var parentWindowClientRect))
            throw new Win32Exception();
            
        var containerHandle = ExternalAppsNative.CreateShlWnd(
            parentWindowHandle, 
            childWindowHandle, 
            parentWindowClientRect.right - parentWindowClientRect.left, 
            parentWindowClientRect.bottom - parentWindowClientRect.top);

        try
        {
            if (process != null) 
                ProcessJobTracker.AddProcess(process);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, 
                "ProcessJobTracker could not add the process {FileName} with the id {Id}", 
                process?.StartInfo.FileName, 
                process?.Id
                );
        }

        return new HWND(containerHandle);
    }

    internal static HWND DetachWindow(HWND windowHandle)
    {
        var newWindowHandle = PInvoke.SendMessage(windowHandle, PInvoke.WM_PARENTNOTIFY, new WPARAM(PInvoke.WM_NCDESTROY), 0);
        PInvoke.DestroyWindow(windowHandle);
        return new HWND(newWindowHandle);

    }
    
    internal static void ShowSystemMenu(HWND originalWindowHandle, HWND controlHandle, Point point)
    {
        var wMenu = PInvoke.GetSystemMenu(originalWindowHandle, false);
        // Display the menu
        unsafe
        {
            var command = PInvoke.TrackPopupMenuEx(
                wMenu,
                (uint) (TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD), 
                point.X, 
                point.Y, 
                controlHandle);
            
            if (command.Value == 0)
                return;
            
            PInvoke.PostMessage(
                originalWindowHandle, 
                PInvoke.WM_SYSCOMMAND, 
                new WPARAM((nuint)command.Value), 
                IntPtr.Zero);
        }

    }
}