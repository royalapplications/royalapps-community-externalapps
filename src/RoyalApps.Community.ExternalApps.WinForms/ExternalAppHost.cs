// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable StringLiteralTypo

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Win32;

namespace RoyalApps.Community.ExternalApps.WinForms;

/// <summary>
/// The host control which can embed external application windows.
/// </summary>
public class ExternalAppHost : UserControl
{
    //public ILogger Logger { get; set; }
    
    public ExternalAppState ExternalAppState { get; } = new();

    private long _originalGwlStyle;
    private long _embeddedGwlStyle;

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<User32.WinEventDelegate> _winEventProcs = new();
    private readonly List<IntPtr> _winEventHooks = new();

    public event EventHandler<EventArgs>? ApplicationActivated;
    public event EventHandler<EventArgs>? ApplicationStarted;
    public event EventHandler<EventArgs>? ApplicationClosed;
    public event EventHandler<EventArgs>? WindowTitleChanged;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ExternalAppState.Process != null)
            {
                ExternalAppState.Process.Exited -= AppProcess_Exited;
                ExternalAppState.Process = null;
            }

            _winEventHooks.ForEach(delegate(IntPtr hook) { PInvoke.UnhookWinEvent(hook); });
            _winEventHooks.Clear();
            _winEventProcs.Clear();
        }

        base.Dispose(disposing);
    }
    
            public void CloseApplication(bool killProcess = false)
        {
            if (!ExternalAppState.HasWindow || !ExternalAppState.IsRunning)
                return;

            try
            {
                if (killProcess)
                {
                    ExternalAppState.Process?.Kill();
                }
                else
                {
                    // for now we just kill the process
                    if (!ExternalAppState.Process?.CloseMainWindow() ?? true)
                    {
                        ExternalAppState.Process?.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                // Logger.LogWarning(
                //     "Closing external application failed.",
                //     exception: ex);
            }
            finally
            {
                ExternalAppState.ExecutionState = ExecutionState.Stopped;
            }
        }

        public async Task<NativeResult> EmbedApplicationAsync()
        {
            if (!ExternalAppState.HasWindow)
            {
                // process not found or application has been closed
                RaiseApplicationClosed();
                return new NativeResult(false);
            }

            var result = await SetParentAsync(Handle); 
            if (!result.Success)
                return result;

            SetWindowPosition();
            FocusApplication(false);
            return new NativeResult(true);
        }

        public void FocusApplication(bool externalWindowActivation)
        {
            OnFocusApplication(externalWindowActivation);
        }

        public void FreeApplication()
        {
            if (!ExternalAppState.HasWindow)
                return;

        var handle = ExternalAppState.HWND;
            PInvoke.SetParent(handle, new Windows.Win32.Foundation.HWND(IntPtr.Zero));
            PInvoke.SetWindowLong(handle, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)_originalGwlStyle);
            ExternalAppState.IsEmbedded = false;
        }

        public Bitmap? GetWindowScreenshot()
        {
            if (!ExternalAppState.HasWindow)
                return null;
            
            PInvoke.GetWindowRect(ExternalAppState.HWND, out var rect);

            if (rect.Right == 0 || rect.Bottom == 0)
                return null;

            var bmp = new Bitmap(rect.Right, rect.Bottom, PixelFormat.Format32bppArgb);
            var gfxBmp = Graphics.FromImage(bmp);
            var hdcBitmap = gfxBmp.GetHdc();

            PInvoke.PrintWindow(ExternalAppState.WindowHandle, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }

        public void MaximizeApplication()
        {
            if (ExternalAppState.HasWindow)
            {
                PInvoke.ShowWindow(ExternalAppState.WindowHandle, User32.SW_SHOWMAXIMIZED);
            }
        }

        public void SetWindowPosition()
        {
            if (Disposing || IsDisposed)
                return;

            try
            {
                if (ExternalAppState.IsEmbedded)
                {
                    SetWindowPosition(0, 0, Width, Height);
                }
                else
                {
                    PInvoke.ShowWindow(ExternalAppState.WindowHandle, User32.SW_SHOWDEFAULT);
                    SetWindowPosition(new Rectangle(
                        PointToScreen(new Point(Left - SystemInformation.Border3DSize.Width, Top)).X,
                        PointToScreen(new Point(Left - SystemInformation.Border3DSize.Width, Top)).Y,
                        Width,
                        Height));
                }
            }
            catch (Exception ex)
            {
                //Logger.LogWarning("Cannot set the window position.", exception: ex);
            }
        }

        public void SetWindowPosition(Rectangle rectangle)
        {
            SetWindowPosition(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }

        public void SetWindowPosition(int x, int y, int width, int height)
        {
            if (!ExternalAppState.HasWindow)
                return;

            // the coordinates of the client area rectangle
            var rect = new PInvoke.RECT(x, y, x + width, y + height);

            // let windows calculate the best position for the window when we want to have the client rect at those coordinates
            PInvoke.AdjustWindowRectEx(ref rect, (uint)_embeddedGwlStyle, false, 0);

            // let's move the window
            PInvoke.MoveWindow(
                ExternalAppState.WindowHandle,
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top,
                true);
        }

        public async Task<NativeResult> StartApplicationAsync(ExternalAppConfig externalAppConfig)
        {
            if (ExternalAppState.ExecutionState != ExecutionState.Stopped)
                throw new InvalidOperationException("Cannot start application because it is already starting or running.");

            ExternalAppState.ExecutionState = ExecutionState.Starting;
            var result = await StartApplicationInternalAsync(externalAppConfig);
            ExternalAppState.ExecutionState = result.Success 
                ? ExecutionState.Running 
                : ExecutionState.Stopped;
            return result;
        }

        protected virtual void OnApplicationActivated()
        {
            
        }
        
        protected virtual void OnApplicationClosed()
        {
        }

        protected virtual void OnApplicationStarted()
        {
        }

        protected virtual void OnWindowTitleChanged()
        {
        }

        protected virtual void OnFocusApplication(bool externalWindowActivation)
        {
            if (!ExternalAppState.HasWindow)
                return;

            if (ExternalAppState.IsEmbedded || externalWindowActivation)
            {
                Focus();
                PInvoke.SetForegroundWindow(ExternalAppState.WindowHandle);
                PInvoke.SetFocus(ExternalAppState.WindowHandle);
            }
        }

        private void RaiseApplicationActivated()
        {
            LogVerboseInDebugOnly(nameof(RaiseApplicationActivated));
            OnApplicationActivated();
            ApplicationActivated?.Invoke(this, EventArgs.Empty);
        }
        
        private void RaiseApplicationClosed()
        {
            LogVerboseInDebugOnly(nameof(RaiseApplicationClosed));
            OnApplicationClosed();
            ApplicationClosed?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseApplicationStarted()
        {
            LogVerboseInDebugOnly(nameof(RaiseApplicationStarted));
            OnApplicationStarted();
            ApplicationStarted?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseWindowTitleChanged()
        {
            LogVerboseInDebugOnly(nameof(RaiseWindowTitleChanged));
            OnWindowTitleChanged();
            WindowTitleChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            SetWindowPosition();
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case User32.WM_MOUSEACTIVATE:
                case User32.WM_LBUTTONDOWN:
                case User32.WM_MDIACTIVATE:
                case User32.WM_SETFOCUS:
                    // notify host application that the external app area has been clicked
                    RaiseApplicationActivated();
                    // make sure the external application gets the input focus
                    FocusApplication(false);
                    break;
            }
            base.WndProc(ref m);
        }

        private void AppProcess_Exited(object? sender, EventArgs e)
        {
            try
            {
                Invoke(RaiseApplicationClosed);
            }
            catch (Exception ex)
            {
                //Logger.LogWarning(exception: ex);
            }
        }

                private async Task<NativeResult> FindProcessAsync(ExternalAppConfig externalAppConfig)
        {
            if (string.IsNullOrWhiteSpace(externalAppConfig.ProcessNameToTrack) &&
                string.IsNullOrWhiteSpace(externalAppConfig.CommandLineMatchString))
            {
                // no need to search for anything, bail...
                return new NativeResult(true);
            }

            var commandFound = false;

            // setup WMI
            var wmiQuery = "SELECT ProcessId, CommandLine FROM Win32_Process";
            if (!string.IsNullOrWhiteSpace(externalAppConfig.ProcessNameToTrack))
                wmiQuery += $" WHERE Name='{externalAppConfig.ProcessNameToTrack}'";
            var searcher = new ManagementObjectSearcher(wmiQuery);
            //Logger.LogDebug(details: $"WMI query to execute: {wmiQuery}");

            var debugInfoStringBuilder = new StringBuilder();
            debugInfoStringBuilder.AppendLine($"Process name to track: {externalAppConfig.ProcessNameToTrack}");
            debugInfoStringBuilder.AppendLine($"Command line fragment to look for: {externalAppConfig.CommandLineMatchString}");

            // assign local variables to prevent leaking memory by capturing members in async methods
            var minWaitTimeInMs = externalAppConfig.MinWaitTime * 1000;
            var maxWaitTime = externalAppConfig.MaxWaitTime;
            var commandLineMatchString = externalAppConfig.CommandLineMatchString;

            var currentProcess = ExternalAppState.Process;
            var windowHandle = ExternalAppState.WindowHandle;

            await System.Threading.Tasks.Task.Run(() =>
            {
                // first, wait the MinWaitTime, before we look for a process or command line
                Thread.Sleep(minWaitTimeInMs);
                var tryCount = 0;
                while (true)
                {
                    foreach (var managementBaseObject in searcher.Get())
                    {
                        if (managementBaseObject is not ManagementObject wmiWin32Process)
                            continue;

                        var commandLine = wmiWin32Process["CommandLine"] != null
                            ? wmiWin32Process["CommandLine"].ToString()
                            : string.Empty;
                        var processIdString = wmiWin32Process["ProcessId"] != null
                            ? wmiWin32Process["ProcessId"].ToString()
                            : string.Empty;

                        // check if we can parse the process id and if we have a valid command line
                        if (!int.TryParse(processIdString, out var processId))
                            continue;

                        if (string.IsNullOrWhiteSpace(commandLine) || processId <= -1)
                            continue;

                        debugInfoStringBuilder.AppendLine($"Process: {processIdString}, Command Line: {commandLine}");

                        // if no command line match string has been provided, take the process we found
                        // or in case a command line match string has been provided, check if it matches
                        if (string.IsNullOrWhiteSpace(commandLineMatchString) ||
                            commandLine.ToLower().Contains(commandLineMatchString.ToLower()))
                        {
                            if (currentProcess != null)
                                currentProcess.Exited -= AppProcess_Exited;
                            currentProcess = Process.GetProcessById(processId);
                            windowHandle = currentProcess.MainWindowHandle;
                            commandFound = true;
                            break;
                        }
                    }

                    tryCount++;
                    Thread.Sleep(250);

                    if (commandFound || tryCount >= maxWaitTime * 4)
                        break;
                }
            });

            ExternalAppState.Process = currentProcess;
            ExternalAppState.WindowHandle = windowHandle;

            // Logger<>.LogDebug(
            //     commandFound
            //         ? $"FindProcessAsync succeeded."
            //         : $"FindProcessAsync failed.",
            //     details: debugInfoStringBuilder.ToString());

            return commandFound 
                ? new NativeResult(true) 
                : new NativeResult(false);
        }

        private async Task<bool> FindWindowTitleAsync(ExternalAppConfig externalAppConfig)
        {
            Thread.Sleep(externalAppConfig.MinWaitTime * 1000);

            var tryCountMatch = 0;
            var titleMatches = 0;
            var windowFound = false;

            var maxWaitTime = externalAppConfig.MaxWaitTime;
            var windowTitleMatch = externalAppConfig.WindowTitleMatch;
            var windowTitleMatchSkip = externalAppConfig.WindowTitleMatchSkip;
            var windowHandle = ExternalAppState.WindowHandle;
            var currentProcess = ExternalAppState.Process;

            if (currentProcess == null)
                return false;

            await System.Threading.Tasks.Task.Run(() =>
            {
                do
                {
                    if (tryCountMatch >= maxWaitTime * 4)
                        break;

                    Thread.Sleep(250);

                    foreach (var window in DesktopWindow.GetDesktopWindows())
                    {
                        if (window.ProcessId != currentProcess.Id)
                            continue;

                        // title matches
                        if (!window.Title.Contains(windowTitleMatch))
                            continue;

                        // we are still on the same window
                        if (windowTitleMatchSkip > 0 && windowHandle == window.Handle)
                            continue;

                        // if window title matches should be skipped
                        if (titleMatches <= windowTitleMatchSkip)
                        {
                            titleMatches++;
                            break;
                        }

                        // remember the current window handle
                        windowHandle = window.Handle;

                        // window finally found
                        windowFound = true;
                        break;
                    }

                    tryCountMatch++;
                } while (currentProcess.HasExited == false && !windowFound);
            });

            ExternalAppState.WindowHandle = windowHandle;

            return windowFound;
        }

        private IntPtr GetWindowHandleFromPicker()
        {
            var windowHandle = IntPtr.Zero;
            var singleForm = new PropertyDialog
            {
                Text = "Window &Picker...".TLL().StripAccelerator(),
                HeaderImageSvg = ImageCache.GetSvgImageLarge(AppResources.SvgIcons.ActionOpenApplicationFolder),
                HeaderText = "External application window not found".TLL(),
                HeaderSubText = "Can the Window be found in the list below?".TLL(),
                NavigationVisible = false,
                ButtonBackNextVisible = false
            };

            singleForm.AddPropertyPage(
                DiContainer.Resolve<Func<WindowPickerPage>>(),
                WindowPickerPage.GetPanelName(),
                string.Empty,
                WindowPickerPage.GetTreeSvg());
            singleForm.ActivatePropertyPage(WindowPickerPage.GetPanelName());
            var dialogResult = singleForm.ShowDialog(AppInfo.MainWindow);

            if (singleForm.ObjectToEdit is Tuple<string, string, string, string, IntPtr> pickedWindow)
                windowHandle = pickedWindow.Item5;

            if (dialogResult != DialogResult.OK || windowHandle == IntPtr.Zero)
                return IntPtr.Zero;

            return windowHandle;
        }

        private async Task<NativeResult> SetParentAsync(IntPtr parentHandle)
        {
            var result = new NativeResult(false);
            var retry = 0;

            // remember the original window style (currently not in use because application of old style doesn't always work)
            _originalGwlStyle = User32.GetWindowLong(ExternalAppState.WindowHandle, User32.GWL_STYLE);
            // setting these styles don't work because keyboard input is broken afterwards
            var newStyle = _originalGwlStyle & ~(User32.WS_GROUP|User32.WS_TABSTOP) | User32.WS_CHILD;
            User32.SetWindowLong(ExternalAppState.WindowHandle, User32.GWL_STYLE, newStyle);

            //var logger = Logger;

            // this needs to run asynchronously to not block the UI thread
            await System.Threading.Tasks.Task.Run(() =>
            {
                do
                {
                    try
                    {
                        result = SetParentInternal(parentHandle);
                    }
                    catch (Exception ex)
                    {
                        result = new NativeResult(false, ex);
                        //logger.LogDebug("SetParentInternal failed.", exception: ex);
                    }

                    if (result.Success || retry > 10)
                        break;

                    retry++;
                    Thread.Sleep(100);
                } while (true);
            });

            if (result.Success)
            {
                _embeddedGwlStyle = PInvoke.GetWindowLong(ExternalAppState.WindowHandle, User32.GWL_STYLE);
            }

            ExternalAppState.IsEmbedded = result.Success;
            return result;
        }

        private NativeResult SetParentInternal(IntPtr parentHandle)
        {
            var windowHandle = ExternalAppState.WindowHandle;

            // BeginInvoke is needed here!
            // When not executed at the end of the message pump, the SetParent call always returns '5' (Access Denied)
            var asyncResult = BeginInvoke(new MethodInvoker(() =>
            {
                // https://devblogs.microsoft.com/oldnewthing/?p=4683
                PInvoke.SetParent(windowHandle, parentHandle);
            }));

            // we need to wait for the async code to finish and get the last win32 error code
            EndInvoke(asyncResult);
            
            var lastWin32Exception = new Win32Exception();
            var success = lastWin32Exception.NativeErrorCode == 0;
            //Logger.LogDebug($"SetParentInternal success: {success}, Error Code: {lastWin32Exception.NativeErrorCode}");
            return success 
                ? new NativeResult(true)
                : new NativeResult(false, lastWin32Exception);
        }

        private void SetupHooks()
        {
            if (!ExternalAppState.IsRunning)
                return;

            var winEventProc = new User32.WinEventDelegate(WinEventProc);
            // always add the delegate to the _winEventProcs list
            // otherwise GC will dump this delegate and the hook fails
            _winEventProcs.Add(winEventProc);
            var eventType = (uint)User32.WinEvents.EVENT_OBJECT_NAMECHANGE;
            _winEventHooks.Add(
                PInvoke.SetWinEventHook(
                    eventType, 
                    eventType, 
                    IntPtr.Zero, 
                    winEventProc, 
                    (uint)ExternalAppState.Process!.Id, 
                    0, 
                    User32.WINEVENT_OUTOFCONTEXT));
        }

        private async Task<NativeResult> StartApplicationInternalAsync(ExternalAppConfig externalAppConfig)
        {
            #region --- Start Process (if needed) ---
            var result = await StartProcessAsync(externalAppConfig);
            if (!result.Success)
                return result;
            #endregion

            #region --- Search for a specific process or command line (if configured) ---
            result = await FindProcessAsync(externalAppConfig);
            // if (!result.Success)
            //     Logger.LogWarning("FindProcessAsync raised an error.", exception: result.Exception);

            result = new NativeResult(false);
            #endregion

            #region --- Search for a specific window title (if configured) ---
            if (!string.IsNullOrEmpty(externalAppConfig.WindowTitleMatch))
            {
                var windowFound = await FindWindowTitleAsync(externalAppConfig);
                if (!windowFound)
                {
                    ExternalAppState.WindowHandle = GetWindowHandleFromPicker();
                    if (!ExternalAppState.HasWindow)
                    {
                        RaiseApplicationClosed();
                        return new NativeResult(false);
                    }
                }
            }
            #endregion

            if (ExternalAppState.Process != null)
            {
                ExternalAppState.Process.EnableRaisingEvents = true;
                ExternalAppState.Process.Exited += AppProcess_Exited;
            }

            if (!externalAppConfig.StartExternal)
            {
                try
                {
                    result = await EmbedApplicationAsync();
                }
                catch (Exception ex)
                {
                    //Logger.LogWarning("Embedding application failed.", exception: ex);
                }
            }

            if (!result.Success)
                //Logger.LogWarning("StartApplicationInternalAsync raised an error.", exception: result.Exception);
            
            SetupHooks();
            SetWindowPosition();
            RaiseApplicationStarted();

            return new NativeResult(true);
        }

        private async Task<NativeResult> StartProcessAsync(ExternalAppConfig externalAppConfig)
        {
            if (externalAppConfig.UseExistingProcess)
                return new NativeResult(true);

            #region --- Prepare Process for Execution and Start ---

            try
            {
                ExternalAppState.StartProcess(externalAppConfig);
            }
            catch (Exception ex)
            {
                return new NativeResult(false, ex);
            }

            if (ExternalAppState.Process == null)
            {
                RaiseApplicationClosed();
                return new NativeResult(false);
            }

            try
            {
                ExternalAppState.Process.WaitForInputIdle();
            }
            catch
            {
                // ignore: PowerShell doesn't allow to wait for input idle
            }

            if (!ExternalAppState.IsRunning)
            {
                RaiseApplicationClosed();
                return new NativeResult(false, new Exception("The process is not running anymore or does not have a main window handle."));
            }

            #endregion

            #region --- Wait for the App to be started, try to get the main window handle ---

            // depending on the configuration, we don't always need the main window handle of the process we started

            var maxWaitTime = externalAppConfig.MaxWaitTime;
            var currentProcess = ExternalAppState.Process;

            Exception? lastException = null;
            
            await System.Threading.Tasks.Task.Run(() =>
            {
                var tryCount = 0;
                var mainWindowHandle = IntPtr.Zero;
                var mainWindowTitle = "Default IME";
                do
                {
                    if (tryCount >= maxWaitTime * 4)
                        break;

                    currentProcess.Refresh();

                    Thread.Sleep(100);

                    tryCount++;

                    try
                    {
                        mainWindowHandle = currentProcess.MainWindowHandle;
                        mainWindowTitle = currentProcess.MainWindowTitle;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                } while (!currentProcess.HasExited &&
                         (mainWindowHandle == IntPtr.Zero || mainWindowTitle == "Default IME"));
            });

            ExternalAppState.Process = currentProcess;

            if (!ExternalAppState.IsRunning || lastException != null)
            {
                RaiseApplicationClosed();
                return new NativeResult(false, lastException);
            }

            ExternalAppState.WindowHandle = ExternalAppState.Process.MainWindowHandle;

            #endregion

            return new NativeResult(true);
        }

        // ReSharper disable once IdentifierTypo
        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            LogVerboseInDebugOnly($"WinEventProc: EventType: {eventType}, Window Handle: {hWnd}, idObject: {idObject}, idChild: {idChild}");

            if (ExternalAppState.WindowHandle != hWnd)
            {
                LogVerboseInDebugOnly($"WinEventProc: exiting because WindowHandle ({ExternalAppState.WindowHandle}) != hWnd ({hWnd})");
                return;
            }
            
            switch (eventType)
            {
                case (uint)User32.WinEvents.EVENT_OBJECT_NAMECHANGE:
                    LogVerboseInDebugOnly($"WinEventProc: EVENT_OBJECT_NAMECHANGE: {ExternalAppState.GetWindowTitle()}");
                    InvokeIfRequired(RaiseWindowTitleChanged);
                    break;
            }
        }

        private void LogVerboseInDebugOnly(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            //Logger.LogVerbose(message, null, null, null, memberName, sourceFilePath, sourceLineNumber);
            Console.WriteLine(message);
        }
}