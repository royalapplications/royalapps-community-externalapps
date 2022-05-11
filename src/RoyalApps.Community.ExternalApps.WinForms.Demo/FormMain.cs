using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

public partial class FormMain : Form
{
    public FormMain()
    {
        InitializeComponent();
        TabControlLeft.TabPages.Clear();
        TabControlRight.TabPages.Clear();
    }

    private void MenuItemExit_Click(object sender, EventArgs e)
    {
        // TODO: close all apps
        Close();
    }

    private void MenuItemEmbed_Click(object sender, EventArgs e)
    {
    }

    private void ExternalApp_ApplicationStarted(object? sender, EventArgs e)
    {
    }

    private void ExternalApp_ApplicationClosed(object? sender, EventArgs e)
    {
        if (sender is not ExternalAppHost {Parent: TabPage tabPage} externalAppHost)
            return;

        externalAppHost.ApplicationStarted -= ExternalApp_ApplicationStarted;
        externalAppHost.ApplicationClosed -= ExternalApp_ApplicationClosed;

        TabControlLeft.TabPages.Remove(tabPage);
        //AddLogEntry($"Application closed: {externalAppHost}");
    }

    private void StripButtonAddLeft_Click(object sender, EventArgs e)
    {
        AddApplication(TabControlLeft, new ExternalAppConfiguration {Command = StripTextBoxQuickEmbed.Text});
    }

    private void StripButtonAddRight_Click(object sender, EventArgs e)
    {
        AddApplication(TabControlRight, new ExternalAppConfiguration {Command = StripTextBoxQuickEmbed.Text});
    }

    private void StripMenuItemAsChild_Click(object? sender, EventArgs e)
    {
        StripMenuItemAsChild.Checked = !StripMenuItemAsChild.Checked;
    }

    private void AddApplication(TabControl tabControl, ExternalAppConfiguration externalAppConfiguration)
    {
        if (string.IsNullOrWhiteSpace(externalAppConfiguration.Command))
        {
            AddLogEntry($"No executable specified");
            return;
        }
        if (!File.Exists(externalAppConfiguration.Command))
        {
            AddLogEntry($"Executable not found: {externalAppConfiguration.Command}");
            return;
        }

        var fileInfo = new FileInfo(externalAppConfiguration.Command!);
        var caption = fileInfo.Name;
        
        var tabPage = new TabPage(caption);
        var externalApp = new ExternalAppHost
        {
            Dock = DockStyle.Fill,
            Parent = tabPage,
        };
        externalApp.ApplicationStarted += ExternalApp_ApplicationStarted;
        externalApp.ApplicationClosed += ExternalApp_ApplicationClosed;

        tabControl.TabPages.Add(tabPage);

        AddLogEntry($"Starting application: {externalAppConfiguration.Command}");
        externalApp.Start(externalAppConfiguration);
    }

    private void AddLogEntry(string message)
    {
        TextBoxLog.Text += $@"{Environment.NewLine}{DateTime.Now.ToShortTimeString()}: {message}";
    }

}