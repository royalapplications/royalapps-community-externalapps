using System.Drawing;
using Windows.Win32.UI.WindowsAndMessaging;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class WindowBoundsCalculatorTests
{
    [Fact]
    public void GetEmbeddedWindowBounds_ReturnsRequestedBounds_WhenWindowChromeIsIgnored()
    {
        var requestedBounds = new Rectangle(10, 20, 300, 200);

        var actualBounds = WindowBoundsCalculator.GetEmbeddedWindowBounds(
            requestedBounds,
            WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_THICKFRAME,
            includeWindowChromeDimensions: false);

        Assert.Equal(requestedBounds, actualBounds);
    }

    [Fact]
    public void GetEmbeddedWindowBounds_ExpandsBounds_WhenWindowChromeIsIncluded()
    {
        var requestedBounds = new Rectangle(10, 20, 300, 200);

        var actualBounds = WindowBoundsCalculator.GetEmbeddedWindowBounds(
            requestedBounds,
            WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_THICKFRAME,
            includeWindowChromeDimensions: true);

        Assert.True(actualBounds.X <= requestedBounds.X);
        Assert.True(actualBounds.Y <= requestedBounds.Y);
        Assert.True(actualBounds.Width >= requestedBounds.Width);
        Assert.True(actualBounds.Height >= requestedBounds.Height);
        Assert.NotEqual(requestedBounds, actualBounds);
    }
}
