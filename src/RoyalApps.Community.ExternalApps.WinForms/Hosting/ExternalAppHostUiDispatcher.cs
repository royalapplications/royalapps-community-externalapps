using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoyalApps.Community.ExternalApps.WinForms.Hosting;

internal sealed class ExternalAppHostUiDispatcher
{
    private readonly Control _control;

    public ExternalAppHostUiDispatcher(Control control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
    }

    public void InvokeIfRequired(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!CanDispatch())
            return;

        try
        {
            if (_control.InvokeRequired)
            {
                _control.BeginInvoke(action);
                return;
            }

            action();
        }
        catch (InvalidOperationException) when (!CanDispatch())
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!await WaitForHandleAsync(cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            if (_control.InvokeRequired)
            {
                await _control.InvokeAsync(action, cancellationToken).ConfigureAwait(false);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            action();
        }
        catch (InvalidOperationException) when (!CanDispatch())
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async Task InvokeAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!await WaitForHandleAsync(cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            if (_control.InvokeRequired)
            {
                await _control.InvokeAsync(
                    async dispatchCancellationToken => await action(dispatchCancellationToken),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await action(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException) when (!CanDispatch())
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private bool CanDispatch()
    {
        return !_control.IsDisposed && _control.IsHandleCreated;
    }

    private async Task<bool> WaitForHandleAsync(CancellationToken cancellationToken)
    {
        if (_control.IsDisposed)
            return false;

        if (_control.IsHandleCreated)
            return true;

        var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler? handleCreatedHandler = null;
        EventHandler? disposedHandler = null;

        handleCreatedHandler = (_, _) => taskCompletionSource.TrySetResult(true);
        disposedHandler = (_, _) => taskCompletionSource.TrySetResult(false);

        _control.HandleCreated += handleCreatedHandler;
        _control.Disposed += disposedHandler;

        try
        {
            if (_control.IsDisposed)
                return false;

            if (_control.IsHandleCreated)
                return true;

            using var registration = cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken));
            return await taskCompletionSource.Task.ConfigureAwait(false);
        }
        finally
        {
            _control.HandleCreated -= handleCreatedHandler;
            _control.Disposed -= disposedHandler;
        }
    }
}
