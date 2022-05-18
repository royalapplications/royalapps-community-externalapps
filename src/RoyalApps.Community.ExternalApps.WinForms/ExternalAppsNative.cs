using System;
using System.Runtime.InteropServices;

namespace RoyalApps.Community.ExternalApps.WinForms;

internal class ExternalAppsNative
{
    [DllImport(@"lib\WinEmbed.dll", SetLastError = true)]
    public static extern void InitShl();
    
    [DllImport(@"lib\WinEmbed.dll", SetLastError = true)]
    public static extern void DoneShl();

    [DllImport(@"lib\WinEmbed.dll", SetLastError = true)]
    public static extern IntPtr CreateShlWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height);
}