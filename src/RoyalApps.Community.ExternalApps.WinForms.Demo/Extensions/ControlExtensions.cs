using System.Drawing;
using System.Windows.Forms;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo.Extensions;

public static class ControlExtensions
{
    /// <summary>
    /// Finds the currently focused control.
    /// </summary>
    /// <param name="control">The form or control you want to find the active/focused control in.</param>
    /// <returns>The focused control.</returns>
    public static Control FindFocusedControl(this Control control)
    {
        var container = control as ContainerControl;
        while (container != null)
        {
            if (container.ActiveControl == null)
                break;
            control = container.ActiveControl;
            container = control as ContainerControl;
        }
        return control;
    }

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
