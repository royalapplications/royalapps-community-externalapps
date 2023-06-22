using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.Demo.Extensions;
using RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

public partial class FormMain : Form
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FormMain> _logger;

    private readonly Timer _logWriterTimer = new();

    public FormMain(ILoggerFactory loggerFactory)
    {
        InitializeComponent();

        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<FormMain>();

        _logWriterTimer.Tick += (_, _) =>
        {
            var text = Program.ConsoleOutput.GetStringBuilder().ToString();
            if (text.Length == TextBoxLog.TextLength)
                return;
            TextBoxLog.Text = text;
            TextBoxLog.SelectionStart = TextBoxLog.TextLength;
            TextBoxLog.ScrollToCaret();
        };
        _logWriterTimer.Interval = 500;
        _logWriterTimer.Enabled = true;
        
        TabControlLeft.TabPages.Clear();
        TabControlRight.TabPages.Clear();
    }

    private void MenuItemExit_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void MenuItemApplication_DropDownOpening(object sender, EventArgs e)
    {
        MenuItemEmbed.Enabled = false;
        MenuItemDetach.Enabled = false;
        MenuItemApplication.Tag = null;

        if (ActiveControl?.FindFocusedControl() is not ExternalAppHost externalAppHost)
            return;

        MenuItemApplication.Tag = externalAppHost;
        MenuItemEmbed.Enabled = !externalAppHost.IsEmbedded;
        MenuItemDetach.Enabled = externalAppHost.IsEmbedded;
    }

    private void MenuItemEmbed_Click(object sender, EventArgs e)
    {
        if (MenuItemApplication.Tag is not ExternalAppHost externalAppHost)
            return;
        externalAppHost.EmbedApplication();
    }

    private void MenuItemDetach_Click(object sender, EventArgs e)
    {
        if (MenuItemApplication.Tag is not ExternalAppHost externalAppHost)
            return;
        externalAppHost.DetachApplication();
    }

    private void ExternalApp_ApplicationStarted(object? sender, EventArgs e)
    {
        if (sender is not ExternalAppHost { Parent: TabPage { Parent: TabControl }} externalAppHost)
            return;

        if (externalAppHost.Configuration == null)
            return;

        _logger.LogInformation("Starting application: {Command}", externalAppHost.Configuration.Executable);
    }

    private void ExternalApp_ApplicationClosed(object? sender, EventArgs e)
    {
        if (sender is not ExternalAppHost {Parent: TabPage {Parent: TabControl tabControl} tabPage} externalAppHost)
            return;

        externalAppHost.ApplicationStarted -= ExternalApp_ApplicationStarted;
        externalAppHost.ApplicationClosed -= ExternalApp_ApplicationClosed;

        tabControl.TabPages.Remove(tabPage);
    }

    private void StripButtonAddLeft_Click(object sender, EventArgs e)
    {
        AddApplication(TabControlLeft, new ExternalAppConfiguration {Executable = StripTextBoxQuickEmbed.Text});
    }

    private void StripButtonAddRight_Click(object sender, EventArgs e)
    {
        AddApplication(TabControlRight, new ExternalAppConfiguration {Executable = StripTextBoxQuickEmbed.Text});
    }

    private void MenuItemControl_Click(object? sender, EventArgs e)
    {
        MenuItemControl.Checked = true;
        MenuItemWindow.Checked = false;
        StripDropDownButtonAdd.ShowDropDown();
    }

    private void MenuItemWindow_Click(object sender, EventArgs e)
    {
        MenuItemControl.Checked = false;
        MenuItemWindow.Checked = true;
        StripDropDownButtonAdd.ShowDropDown();
    }

    private void AddApplication(TabControl tabControl, ExternalAppConfiguration externalAppConfiguration)
    {
        if (string.IsNullOrWhiteSpace(externalAppConfiguration.Executable))
        {
            _logger.LogWarning("No executable specified");
            return;
        }
        if (!File.Exists(externalAppConfiguration.Executable))
        {
            _logger.LogError("Executable not found: {Command}", externalAppConfiguration.Executable);
            return;
        }

        Program.ConsoleOutput.GetStringBuilder().Clear();

        externalAppConfiguration.EmbedMethod = MenuItemControl.Checked 
            ? EmbedMethod.Control 
            : EmbedMethod.Window;
        
        var fileInfo = new FileInfo(externalAppConfiguration.Executable!);
        var caption = fileInfo.Name;
        
        var tabPage = new TabPage(caption);
        var externalApp = new ExternalAppHost
        {
            Dock = DockStyle.Fill,
            LoggerFactory = _loggerFactory,
            Parent = tabPage,
        };
        externalApp.ApplicationStarted += ExternalApp_ApplicationStarted;
        externalApp.ApplicationClosed += ExternalApp_ApplicationClosed;

        tabControl.TabPages.Add(tabPage);
        tabControl.SelectedTab = tabPage;

        _logger.LogInformation("Starting application: {Command}", externalAppConfiguration.Executable);
        externalApp.Start(externalAppConfiguration);
    }

    private void TabControl_MouseUp(object? sender, MouseEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;
        var tabIndex = tabControl.GetTabIndex(e.Location);
        if (tabIndex == -1)
            return;

        _logger.LogInformation("Tab: {TabIndex} was selected", tabIndex);

        var tab = tabControl.TabPages[tabIndex];
        if (tab.Controls[0] is ExternalAppHost externalAppHost)
            BeginInvoke(externalAppHost.Focus);
    }
}