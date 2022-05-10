﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using Windows.Win32;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement
{
    internal class ProcessWindowProvider
    {

        private ILogger _logger;

        public ProcessWindowProvider(ILogger? logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public IEnumerable<ProcessWindowInfo> GetProcessWindows()
        {
            var windows = new List<ProcessWindowInfo>();
            BOOL Filter(HWND hWnd, LPARAM lParam)
            {
                var capLength = PInvoke.GetWindowTextLength(hWnd);
                var pwstr = new PWSTR();
                var nLength = PInvoke.GetWindowText(hWnd, pwstr, capLength);
                var strTitle = pwstr.AsSpan().ToString();

                if (!PInvoke.IsWindowVisible(hWnd) || string.IsNullOrEmpty(strTitle))
                    return true;

                int pid = 0;
                unsafe 
                {
                    uint* pidPtr = null;
                    PInvoke.GetWindowThreadProcessId(hWnd, pidPtr);
                    pid = (int)*pidPtr;
                };

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

            var result = PInvoke.EnumDesktopWindows(new Windows.Win32.System.StationsAndDesktops.HDESK(IntPtr.Zero), Filter, new LPARAM(IntPtr.Zero));

            if (!result)
            {
                _logger.LogWarning("Unable to enumerate all desktop windows");
            }

            return windows;
        }

        private string GetProcessExe(Process process)
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
}
