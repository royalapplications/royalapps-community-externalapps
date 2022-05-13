using System.Drawing;
using System.Windows.Forms;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo.Extensions;

public static class TabControlExtensions
{
    /// <summary>
    /// Determine which tab index is at specified location
    /// </summary>
    /// <param name="tabControl">The tab control in question.</param>
    /// <param name="point">The location.</param>
    /// <returns>The tab index at the location. Returns -1 if the location is no match.</returns>
    public static int GetTabIndex(this TabControl tabControl, Point point)
    {
        for (var i = 0; i < tabControl.TabPages.Count; i++)
        {
            if (tabControl.GetTabRect(i).Contains(point))
                return i;
        }

        return -1;
    }

}