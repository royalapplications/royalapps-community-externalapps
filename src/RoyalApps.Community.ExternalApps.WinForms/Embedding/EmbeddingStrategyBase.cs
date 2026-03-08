using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Options;

namespace RoyalApps.Community.ExternalApps.WinForms.Embedding;

internal abstract class EmbeddingStrategyBase : IEmbeddingStrategy
{
    private WINDOW_STYLE _embeddedStyle;
    private WINDOW_STYLE _originalStyle;

    protected EmbeddingStrategyBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected ILogger Logger { get; }

    public async Task EmbedAsync(ExternalAppHost ownerControl, HWND windowHandle, ExternalAppEmbeddingOptions options, CancellationToken cancellationToken)
    {
        var parentWindowHandle = EnsureParentWindow(ownerControl);
        await SetParentAsync(parentWindowHandle, windowHandle, cancellationToken);
        PositionEmbeddedWindow(windowHandle, GetEmbeddedBounds(ownerControl.ClientRectangle), options);
    }

    public virtual void Detach(HWND windowHandle)
    {
        if (windowHandle == HWND.Null)
            return;

        try
        {
            PInvoke.SetParent(windowHandle, HWND.Null);
            _originalStyle &= ~WINDOW_STYLE.WS_DISABLED;
            PInvoke.SetWindowLong(windowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)_originalStyle);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Detaching the embedded window failed");
        }
    }

    public void SetWindowPosition(HWND windowHandle, Rectangle bounds, bool isEmbedded, ExternalAppEmbeddingOptions options)
    {
        if (windowHandle == HWND.Null)
            return;

        if (!isEmbedded)
        {
            PInvoke.MoveWindow(windowHandle, bounds.X, bounds.Y, bounds.Width, bounds.Height, true);
            return;
        }

        PositionEmbeddedWindow(windowHandle, GetEmbeddedBounds(bounds), options);
    }

    public virtual void Dispose()
    {
    }

    protected abstract HWND EnsureParentWindow(ExternalAppHost ownerControl);

    protected virtual Rectangle GetEmbeddedBounds(Rectangle requestedBounds)
    {
        return requestedBounds;
    }

    protected void PositionEmbeddedWindow(HWND windowHandle, Rectangle bounds, ExternalAppEmbeddingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var targetBounds = WindowBoundsCalculator.GetEmbeddedWindowBounds(
            bounds,
            _embeddedStyle,
            options.IncludeWindowChromeDimensions);
        PInvoke.MoveWindow(
            windowHandle,
            targetBounds.X,
            targetBounds.Y,
            targetBounds.Width,
            targetBounds.Height,
            true);
    }

    private async Task SetParentAsync(HWND parentWindowHandle, HWND childWindowHandle, CancellationToken cancellationToken)
    {
        var retry = 0;
        bool success;
        Win32Exception? lastWin32Exception = null;

        _originalStyle = (WINDOW_STYLE)PInvoke.GetWindowLong(childWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        var newStyle = _originalStyle & ~(WINDOW_STYLE.WS_GROUP | WINDOW_STYLE.WS_TABSTOP) | WINDOW_STYLE.WS_CHILD;
        PInvoke.SetWindowLong(childWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)newStyle);

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                PInvoke.SetParent(childWindowHandle, parentWindowHandle);
                lastWin32Exception = new Win32Exception();
                success = lastWin32Exception.NativeErrorCode == 0;

                Logger.LogDebug(
                    success ? null : lastWin32Exception,
                    "SetParent success: {Success}, Error Code: {NativeErrorCode}",
                    success,
                    lastWin32Exception.NativeErrorCode);
            }
            catch (Exception ex)
            {
                success = false;
                Logger.LogDebug(ex, "SetParent failed");
            }

            if (success || retry > 10 || IsNonRetriableSetParentError(lastWin32Exception))
                break;

            retry++;
            await Task.Delay(250, cancellationToken);
        } while (true);

        if (success)
        {
            _embeddedStyle = (WINDOW_STYLE)PInvoke.GetWindowLong(childWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            return;
        }

        PInvoke.SetWindowLong(childWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)_originalStyle);
        throw new EmbeddingFailedException(
            "The selected window could not be reparented. The window will need to remain external.",
            lastWin32Exception?.NativeErrorCode,
            lastWin32Exception);
    }

    private static bool IsNonRetriableSetParentError(Win32Exception? lastWin32Exception)
    {
        return lastWin32Exception?.NativeErrorCode == 87;
    }
}
