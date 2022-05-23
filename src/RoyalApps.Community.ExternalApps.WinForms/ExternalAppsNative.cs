using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms;

internal static class ExternalAppsNative
{
    [DllImport(@"WinEmbed.dll", SetLastError = true)]
    public static extern void InitShl();
    
    [DllImport(@"WinEmbed.dll", SetLastError = true)]
    public static extern void DoneShl();

    [DllImport(@"WinEmbed.dll", SetLastError = true)]
    public static extern IntPtr CreateShlWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height);
}