using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.StationsAndDesktops;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

internal sealed class ProcessWindowProvider
{
    private readonly ILogger _logger;

    public ProcessWindowProvider(ILogger<ProcessWindowProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IEnumerable<ProcessWindowInfo> GetProcessWindows()
    {
        var windows = new List<ProcessWindowInfo>();
        BOOL Filter(HWND hWnd, LPARAM lParam)
        {
            var bufferSize = PInvoke.GetWindowTextLength(hWnd) + 1;
            string? windowName;
            unsafe
            {
                fixed (char* windowNameChars = new char[bufferSize])
                {
                    if (PInvoke.GetWindowText(hWnd, windowNameChars, bufferSize) == 0)
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        if (errorCode != 0)
                            return true;
                    }

                    windowName = new string(windowNameChars);

                    if (!PInvoke.IsWindowVisible(hWnd) || string.IsNullOrEmpty(windowName))
                        return true;
                }
            }
            
            int pid;
            unsafe
            {
                uint pidPtr = 0;
                PInvoke.GetWindowThreadProcessId(hWnd, &pidPtr);
                pid = (int)pidPtr;
            }

            try
            {
                var process = Process.GetProcessById(pid);
                windows.Add(new ProcessWindowInfo(pid, GetProcessExe(process), hWnd, windowName));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Cannot find process with id {Pid}", pid);
            }

            return true;
        }

        var result = PInvoke.EnumDesktopWindows(new HDESK(IntPtr.Zero), Filter, new LPARAM(IntPtr.Zero));

        if (!result)
        {
            _logger.LogWarning("Unable to enumerate all desktop windows");
        }

        return windows;
    }

    private string GetProcessExe(Process? process)
    {
        try
        {
            if (process != null)
            {
                return process.MainModule?.FileName ?? "n/a";
            }
        }
        catch (Win32Exception ex)
        {
            _logger.LogWarning(ex, "Cannot read executable filename for process with id {ProcessId}: {Message}", process?.Id, ex.Message);
        }

        return "n/a";
    }
}