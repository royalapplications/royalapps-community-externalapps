using System;
using System.Diagnostics;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace RoyalApps.Community.ExternalApps.WinForms;

public class ExternalAppState
{
    public ExecutionState ExecutionState { get; set; } = ExecutionState.Stopped;
    public Process? Process { get; set; }
    public ProcessStartInfo? ProcessStartInfo { get; set; }
    public IntPtr WindowHandle { get; set; }
    public HWND HWND => new HWND(WindowHandle);
    public bool IsEmbedded { get; set; }
    public bool HasWindow => WindowHandle != IntPtr.Zero;
    public bool IsRunning => Process is {HasExited: false};
    public string GetWindowTitle()
    {
        if (Process == null || WindowHandle == IntPtr.Zero)
            return string.Empty;
        try
        {
            var handle = new HWND(WindowHandle);
            var capLength = PInvoke.GetWindowTextLength(handle);
            var sb = new StringBuilder(1024);
            User32.GetWindowText(handle, sb, capLength);
            return sb.ToString();
        }
        catch
        {
            // ignored
        }

        return string.Empty;
    }

    public void StartProcess(ExternalAppConfig externalAppConfig)
    {
        ProcessStartInfo = new ProcessStartInfo(
            externalAppConfig.Command ?? string.Empty, 
            externalAppConfig.Arguments ?? string.Empty)
        {
            WindowStyle = externalAppConfig.StartHidden && !externalAppConfig.StartExternal 
                ? ProcessWindowStyle.Minimized
                : ProcessWindowStyle.Normal,
            CreateNoWindow = externalAppConfig.StartHidden && 
                             !externalAppConfig.StartExternal,
            UseShellExecute = true,
            WorkingDirectory = externalAppConfig.WorkingDirectory,
            LoadUserProfile = externalAppConfig.LoadUserProfile
        };

        if (externalAppConfig.RunElevated)
        {
            ProcessStartInfo.UseShellExecute = true;
            ProcessStartInfo.Verb = "runas";
        }
        else if (externalAppConfig.UseCredentials)
        {
            ProcessStartInfo.UseShellExecute = false;
            ProcessStartInfo.Verb = "runas";
            ProcessStartInfo.UserName = externalAppConfig.Username;
            ProcessStartInfo.Domain = externalAppConfig.Domain;
            ProcessStartInfo.Password = SecureStringExtensions.ConvertToSecureString(externalAppConfig.Password);
        }

        Process = Process.Start(ProcessStartInfo);
    }
   
}