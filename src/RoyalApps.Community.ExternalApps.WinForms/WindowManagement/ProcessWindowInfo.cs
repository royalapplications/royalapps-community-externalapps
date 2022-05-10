using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement
{
    internal class ProcessWindowInfo
    {
        public int ProcessId { get; }
        public string ExecutablePath { get; }
        public HWND MainWindowHandle { get; }
        public string WindowTitle { get; }
        public ProcessWindowInfo(int processId, string executablePath, HWND mainWindowHandle, string windowTitle)
        {
            ProcessId = processId;
            ExecutablePath = executablePath;
            MainWindowHandle = mainWindowHandle;
            WindowTitle = windowTitle;
        }
    }
}
