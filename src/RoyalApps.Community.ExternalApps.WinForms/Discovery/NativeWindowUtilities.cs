using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace RoyalApps.Community.ExternalApps.WinForms.Discovery;

internal static class NativeWindowUtilities
{
    public static string GetClassName(HWND windowHandle)
    {
        try
        {
            Span<char> buffer = stackalloc char[256];
            var length = PInvoke.GetClassName(windowHandle, buffer);
            return length > 0
                ? buffer[..length].ToString()
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string GetWindowTitle(HWND windowHandle)
    {
        try
        {
            var length = PInvoke.GetWindowTextLength(windowHandle);
            if (length <= 0)
                return string.Empty;

            var buffer = length + 1 <= 256
                ? stackalloc char[length + 1]
                : new char[length + 1];

            unsafe
            {
                fixed (char* bufferPtr = buffer)
                {
                    var result = PInvoke.GetWindowText(windowHandle, bufferPtr, length + 1);
                    return result > 0
                        ? new string(bufferPtr, 0, result)
                        : string.Empty;
                }
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public static int? GetProcessId(HWND windowHandle)
    {
        unsafe
        {
            uint processId = 0;
            PInvoke.GetWindowThreadProcessId(windowHandle, &processId);
            return processId == 0
                ? null
                : (int)processId;
        }
    }

    public static bool IsTopLevelWindow(HWND windowHandle) =>
        PInvoke.GetWindow(windowHandle, GET_WINDOW_CMD.GW_OWNER) == HWND.Null;
}
