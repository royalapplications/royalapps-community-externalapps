using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

public partial class FormMain : Form
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FormMain> _logger;

    private readonly Timer _logWriterTimer = new Timer();

    public FormMain(ILoggerFactory loggerFactory)
    {
        InitializeComponent();

        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<FormMain>();

        _logWriterTimer.Tick += (sender, args) =>
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

        if (tabPage.Parent is not TabControl tabControl)
            return;
        
        externalAppHost.ApplicationStarted -= ExternalApp_ApplicationStarted;
        externalAppHost.ApplicationClosed -= ExternalApp_ApplicationClosed;

        tabControl.TabPages.Remove(tabPage);
        //AddLogEntry($"Application closed: {externalAppHost}");
    }

    private void StripButtonAddLeft_Click(object sender, EventArgs e)
    {
        AddApplication(TabControlLeft, new ExternalAppConfiguration {Executable = StripTextBoxQuickEmbed.Text});
    }

    private void StripButtonAddRight_Click(object sender, EventArgs e)
    {
        AddApplication(TabControlRight, new ExternalAppConfiguration {Executable = StripTextBoxQuickEmbed.Text});
    }

    private void StripMenuItemAsChild_Click(object? sender, EventArgs e)
    {
        StripMenuItemAsChild.Checked = !StripMenuItemAsChild.Checked;
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

}