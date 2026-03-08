using RoyalApps.Community.ExternalApps.WinForms.Launching;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using System.Threading;
using System.Threading.Tasks;

namespace RoyalApps.Community.ExternalApps.WinForms.Interfaces;

internal interface IProcessLauncher
{
    Task<ProcessLaunchResult> StartAsync(ExternalAppOptions options, CancellationToken cancellationToken);
}
