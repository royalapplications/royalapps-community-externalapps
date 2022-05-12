using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

public partial class FormMain : Form
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<FormMain> logger;

    private readonly Timer LogWriterTimer = new Timer();

    public FormMain(ILoggerFactory loggerFactory)
    {
        InitializeComponent();
        
        this.loggerFactory = loggerFactory;
        logger = loggerFactory.CreateLogger<FormMain>();

        LogWriterTimer.Tick += (sender, args) =>
        {
            TextBoxLog.Text = Program.ConsoleOutput.GetStringBuilder().ToString();
            TextBoxLog.SelectionStart = TextBoxLog.TextLength;
            TextBoxLog.ScrollToCaret();
        };
        LogWriterTimer.Interval = 500;
        LogWriterTimer.Enabled = true;
        
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
            logger.LogWarning("No executable specified");
            return;
        }
        if (!File.Exists(externalAppConfiguration.Command))
        {
            logger.LogError("Executable not found: {Command}", externalAppConfiguration.Command);
            return;
        }

        var fileInfo = new FileInfo(externalAppConfiguration.Command!);
        var caption = fileInfo.Name;
        
        var tabPage = new TabPage(caption);
        var externalApp = new ExternalAppHost
        {
            Dock = DockStyle.Fill,
            LoggerFactory = loggerFactory,
            Parent = tabPage,
        };
        externalApp.ApplicationStarted += ExternalApp_ApplicationStarted;
        externalApp.ApplicationClosed += ExternalApp_ApplicationClosed;

        tabControl.TabPages.Add(tabPage);

        logger.LogInformation("Starting application: {Command}", externalAppConfiguration.Command);
        externalApp.Start(externalAppConfiguration);
    }

}