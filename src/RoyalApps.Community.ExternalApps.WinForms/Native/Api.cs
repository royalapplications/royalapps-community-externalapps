using System;

namespace RoyalApps.Community.ExternalApps.WinForms.Native;

internal abstract class Api
{
    public abstract void InitShell();
    public abstract void DoneShell();
    public abstract IntPtr CreateShellWnd(IntPtr parentHandle, IntPtr childHandle, int width, int height);
}