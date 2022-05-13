using System;
using System.Runtime.InteropServices;

namespace RoyalApps.Community.ExternalApps.WinForms;

internal class ExternalAppsNative
{
    [DllImport(@"lib\swe.dll", SetLastError = true)]
    public static extern void InitShl();
    
    [DllImport(@"lib\swe.dll", SetLastError = true)]
    public static extern void DoneShl();

    [DllImport(@"lib\swe.dll", SetLastError = true)]
    public static extern void CreateShlWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height);
}