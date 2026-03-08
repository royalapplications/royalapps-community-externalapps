using RoyalApps.Community.ExternalApps.WinForms.Selection;
using System.Collections.Generic;

namespace RoyalApps.Community.ExternalApps.WinForms.Interfaces;

internal interface IWindowCatalog
{
    IReadOnlyList<ExternalWindowCandidate> GetAvailableWindows();
}
