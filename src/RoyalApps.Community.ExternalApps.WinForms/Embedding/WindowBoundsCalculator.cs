using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace RoyalApps.Community.ExternalApps.WinForms.Embedding;

internal static class WindowBoundsCalculator
{
    public static Rectangle GetEmbeddedWindowBounds(Rectangle requestedBounds, WINDOW_STYLE embeddedStyle, bool includeWindowChromeDimensions)
    {
        if (!includeWindowChromeDimensions)
            return requestedBounds;

        var rect = new RECT
        {
            left = requestedBounds.X,
            top = requestedBounds.Y,
            right = requestedBounds.X + requestedBounds.Width,
            bottom = requestedBounds.Y + requestedBounds.Height,
        };

        PInvoke.AdjustWindowRectEx(ref rect, embeddedStyle, false, WINDOW_EX_STYLE.WS_EX_LEFT);
        return Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
    }
}
