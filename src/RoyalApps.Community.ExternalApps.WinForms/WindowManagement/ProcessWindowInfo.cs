using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

internal class ProcessWindowInfo
{
    public ProcessWindowInfo(int processId, string executablePath, HWND windowHandle, string windowTitle)
    {
        ProcessId = processId;
        ExecutablePath = executablePath;
        WindowHandle = windowHandle;
        WindowTitle = windowTitle;
    }

    public int ProcessId { get; }

    public string ExecutablePath { get; }

    public string WindowTitle { get; }

    public HWND WindowHandle { get; }
}
