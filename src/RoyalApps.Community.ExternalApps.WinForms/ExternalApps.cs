using RoyalApps.Community.ExternalApps.WinForms.Hosting;
using System;
using System.Diagnostics;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// Setup and initialize external apps hosting.
/// </summary>
public static class ExternalApps
{
    private static readonly ProcessJobTracker ProcessJobTracker = new("ExternalApps");
    private static readonly TrackedWindowRegistry TrackedWindowRegistry = new();

    /// <summary>
    /// Initializes process-tracking services used by external app sessions.
    /// </summary>
    /// <param name="logger">An optional logger used to record initialization details.</param>
    public static void Initialize(ILogger? logger = null)
    {
        logger?.LogDebug("ExternalApps.Initialize completed using managed embedding.");
    }

    /// <summary>
    /// Cleans up process-tracking services before the application host shuts down.
    /// </summary>
    /// <param name="logger">An optional logger used to record cleanup details.</param>
    public static void Cleanup(ILogger? logger = null)
    {
        logger?.LogDebug("ExternalApps.Cleanup completed using managed embedding.");
        ProcessJobTracker.Dispose();
    }

    internal static void TrackProcess(Process process, ILogger logger)
    {
        try
        {
            ProcessJobTracker.AddProcess(process);
        }
        catch (Exception ex)
        {
            string processDescription;
            try
            {
                processDescription = !string.IsNullOrWhiteSpace(process.StartInfo.FileName)
                    ? process.StartInfo.FileName
                    : process.ProcessName;
            }
            catch
            {
                processDescription = process.ProcessName;
            }

            logger.LogWarning(
                ex,
                "ProcessJobTracker could not add the process {ProcessDescription} with the id {Id}",
                processDescription,
                process.Id);
        }
    }

    internal static void RegisterTrackedWindow(HWND windowHandle)
    {
        TrackedWindowRegistry.Register(ToNativeInt(windowHandle));
    }

    internal static void UnregisterTrackedWindow(HWND windowHandle)
    {
        TrackedWindowRegistry.Unregister(ToNativeInt(windowHandle));
    }

    internal static bool IsTrackedWindow(HWND windowHandle)
    {
        return TrackedWindowRegistry.IsTracked(ToNativeInt(windowHandle));
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

    private static unsafe nint ToNativeInt(HWND windowHandle) => (nint)windowHandle.Value;
}
