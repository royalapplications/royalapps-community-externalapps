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

        if (_control.InvokeRequired)
        {
            _control.Invoke(action);
            return;
        }

        action();
    }

    public async Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_control.InvokeRequired)
        {
            await _control.InvokeAsync(action, cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        action();
    }

    public async Task InvokeAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_control.InvokeRequired)
        {
            await _control.InvokeAsync(
                async dispatchCancellationToken => await action(dispatchCancellationToken),
                cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await action(cancellationToken);
    }
}
