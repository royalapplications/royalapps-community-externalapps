using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RoyalApps.Community.ExternalApps.WinForms;

public class User32
{
    // http://msdn.microsoft.com/en-us/library/windows/desktop/ms633520(v=vs.85).aspx
    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    
    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
}