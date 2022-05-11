namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.StationsAndDesktops;

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
            var capLength = PInvoke.GetWindowTextLength(hWnd);
            var lpString = default(PWSTR);
            var nLength = PInvoke.GetWindowText(hWnd, lpString, capLength);
            var strTitle = lpString.AsSpan().ToString();

            if (!PInvoke.IsWindowVisible(hWnd) || string.IsNullOrEmpty(strTitle))
                return true;

            var pid = 0;
            unsafe
            {
                uint* pidPtr = null;
                PInvoke.GetWindowThreadProcessId(hWnd, pidPtr);
                pid = (int)*pidPtr;
            }

            try
            {
                var process = Process.GetProcessById(pid);
                windows.Add(new ProcessWindowInfo(pid, GetProcessExe(process), hWnd, strTitle));
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