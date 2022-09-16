using System;
using System.Runtime.InteropServices;

namespace RoyalApps.Community.ExternalApps.WinForms.Native;

internal sealed class ApiX64 : Api
{
    [DllImport("WinEmbed.x64.dll", SetLastError = true)]
    private static extern void InitShl();

    [DllImport("WinEmbed.x64.dll", SetLastError = true)]
    private static extern void DoneShl();

    [DllImport("WinEmbed.x64.dll", SetLastError = true)]
    private static extern IntPtr CreateShlWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height);

    public override void InitShell()
    {
        InitShl();
    }

    public override void DoneShell()
    {
        DoneShl();
    }

    public override IntPtr CreateShellWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height)
    {
        return CreateShlWnd(parentHandle, childHandle, width, height);
    }
}