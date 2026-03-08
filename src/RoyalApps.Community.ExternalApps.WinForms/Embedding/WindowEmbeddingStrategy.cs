using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace RoyalApps.Community.ExternalApps.WinForms.Embedding;

internal sealed class WindowEmbeddingStrategy : EmbeddingStrategyBase
{
    private EmbeddedWindowContainer? _windowContainer;

    public WindowEmbeddingStrategy(ILogger<EmbeddingController> logger)
        : base(logger)
    {
    }

    public override void Dispose() => DisposeContainer();

    public override void Detach(HWND windowHandle)
    {
        base.Detach(windowHandle);
        DisposeContainer();
    }

    protected override HWND EnsureParentWindow(ExternalAppHost ownerControl)
    {
        ArgumentNullException.ThrowIfNull(ownerControl);

        if (_windowContainer is { IsDisposed: false })
            return _windowContainer.WindowHandle;

        _windowContainer = new EmbeddedWindowContainer
        {
            Parent = ownerControl,
            Bounds = ownerControl.ClientRectangle,
        };
        _windowContainer.BringToFront();
        return _windowContainer.WindowHandle;
    }

    protected override Rectangle GetEmbeddedBounds(Rectangle requestedBounds)
    {
        if (_windowContainer == null)
            return requestedBounds;

        _windowContainer.Bounds = requestedBounds;
        return _windowContainer.ClientRectangle;
    }

    private void DisposeContainer()
    {
        if (_windowContainer == null)
            return;

        var windowContainer = _windowContainer;

        try
        {
            if (windowContainer.IsHandleCreated && windowContainer.InvokeRequired)
            {
                windowContainer.Invoke(windowContainer.Dispose);
            }
            else
            {
                windowContainer.Dispose();
            }
        }
        catch
        {
            // Ignore disposal races on shutdown.
        }
        finally
        {
            _windowContainer = null;
        }
    }

    private sealed class EmbeddedWindowContainer : Control
    {
        public EmbeddedWindowContainer()
        {
            SetStyle(ControlStyles.UserPaint, false);
            SetStyle(ControlStyles.AllPaintingInWmPaint, false);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
            TabStop = false;
            BackColor = SystemColors.Info;
        }

        public HWND WindowHandle => new(Handle);

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.Style |= (int)(WINDOW_STYLE.WS_CLIPCHILDREN | WINDOW_STYLE.WS_CLIPSIBLINGS);
                return createParams;
            }
        }
    }
}
