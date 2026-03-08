using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.Extensions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace RoyalApps.Community.ExternalApps.WinForms.Hosting;

internal sealed class WindowHookManager : IDisposable
{
    private const uint EventObjectFocus = 0x8005;

    private readonly ILogger _logger;
    private readonly List<IntPtr> _winEventHooks = new();
    // Keep native callback delegates alive for the lifetime of the registered hooks.
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<WINEVENTPROC> _winEventProcedures = new();

    private Func<string>? _getWindowTitle;
    private Action<string>? _raiseWindowTitleChanged;
    private Action? _raiseApplicationActivated;

    public WindowHookManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Reset(
        Process? process,
        Func<string> getWindowTitle,
        Action<string> raiseWindowTitleChanged,
        Action raiseApplicationActivated)
    {
        DisposeHooks();

        if (process == null)
            return;

        _getWindowTitle = getWindowTitle ?? throw new ArgumentNullException(nameof(getWindowTitle));
        _raiseWindowTitleChanged = raiseWindowTitleChanged ?? throw new ArgumentNullException(nameof(raiseWindowTitleChanged));
        _raiseApplicationActivated = raiseApplicationActivated ?? throw new ArgumentNullException(nameof(raiseApplicationActivated));

        RegisterWinEventHook(process.Id, PInvoke.EVENT_OBJECT_NAMECHANGE);
        RegisterWinEventHook(process.Id, EventObjectFocus);
    }

    public void Dispose()
    {
        DisposeHooks();
    }

    private void RegisterWinEventHook(int processId, uint eventType)
    {
        var winEventProc = new WINEVENTPROC(WinEventProc);
        _winEventProcedures.Add(winEventProc);

        var hook = PInvoke.SetWinEventHook(
            eventType,
            eventType,
            new HINSTANCE(IntPtr.Zero),
            winEventProc,
            (uint)processId,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT);

        _winEventHooks.Add(hook);
    }

    private void DisposeHooks()
    {
        _winEventHooks.ForEach(hook => PInvoke.UnhookWinEvent(new HWINEVENTHOOK(hook)));
        _winEventHooks.Clear();
        _winEventProcedures.Clear();
        _getWindowTitle = null;
        _raiseWindowTitleChanged = null;
        _raiseApplicationActivated = null;
    }

    private void WinEventProc(
        HWINEVENTHOOK hWinEventHook,
        uint eventType,
        HWND hWnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        switch (eventType)
        {
            case PInvoke.EVENT_OBJECT_NAMECHANGE when _getWindowTitle != null && _raiseWindowTitleChanged != null:
                var caption = _getWindowTitle();
                _logger.WithCallerInfo(logger => logger.LogDebug("WinEventProc: EVENT_OBJECT_NAMECHANGE: {WindowTitle}", caption));
                _raiseWindowTitleChanged(caption);
                break;
            case EventObjectFocus when _raiseApplicationActivated != null:
                _raiseApplicationActivated();
                break;
        }
    }
}
