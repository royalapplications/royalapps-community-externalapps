using System;
using System.Runtime.InteropServices;

namespace RoyalApps.Community.ExternalApps.WinForms;

public class ExternalAppsNative
{
    /// <summary>
    /// 
    /// </summary>
    [DllImport(@"lib\swe.dll", SetLastError = true)]
    public static extern void InitShl();
    
    /// <summary>
    /// 
    /// </summary>
    [DllImport(@"lib\swe.dll", SetLastError = true)]
    public static extern void DoneShl();

    /// <summary>
    /// 
    /// </summary>
    [DllImport(@"lib\swe.dll", SetLastError = true)]
    public static extern void CreateShlWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height);
}