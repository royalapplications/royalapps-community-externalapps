namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

using Windows.Win32.Foundation;

internal class ProcessWindowInfo
{
    public ProcessWindowInfo(int processId, string executablePath, HWND mainWindowHandle, string windowTitle)
    {
        ProcessId = processId;
        ExecutablePath = executablePath;
        MainWindowHandle = mainWindowHandle;
        WindowTitle = windowTitle;
    }

    public int ProcessId { get; }

    public string ExecutablePath { get; }

    public string WindowTitle { get; }

    public HWND MainWindowHandle { get; }
}