using System;
using System.Collections.Concurrent;

namespace RoyalApps.Community.ExternalApps.WinForms.Hosting;

internal sealed class TrackedWindowRegistry
{
    private readonly ConcurrentDictionary<nint, byte> _trackedWindows = new();

    public bool Register(nint windowHandle) =>
        windowHandle != IntPtr.Zero && _trackedWindows.TryAdd(windowHandle, 0);

    public void Unregister(nint windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return;

        _trackedWindows.TryRemove(windowHandle, out _);
    }

    public bool IsTracked(nint windowHandle) =>
        windowHandle != IntPtr.Zero && _trackedWindows.ContainsKey(windowHandle);
}
