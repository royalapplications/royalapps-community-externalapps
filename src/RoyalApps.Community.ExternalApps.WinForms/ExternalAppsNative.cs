using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms;

internal static class ExternalAppsNative
{
#if ARM64
    [DllImport(@"WinEmbed.arm64.dll", SetLastError = true)]
#else
    [DllImport(@"WinEmbed.x64.dll", SetLastError = true)]
#endif
    public static extern void InitShl();

#if ARM64
    [DllImport(@"WinEmbed.arm64.dll", SetLastError = true)]
#else
    [DllImport(@"WinEmbed.x64.dll", SetLastError = true)]
#endif
    public static extern void DoneShl();

#if ARM64
    [DllImport(@"WinEmbed.arm64.dll", SetLastError = true)]
#else
    [DllImport(@"WinEmbed.x64.dll", SetLastError = true)]
#endif
    public static extern IntPtr CreateShlWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height);
}