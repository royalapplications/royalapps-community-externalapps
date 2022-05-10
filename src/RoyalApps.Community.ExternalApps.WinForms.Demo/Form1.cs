namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

public partial class FormMain : Form
{
    public FormMain()
    {
        InitializeComponent();
        TabControl.TabPages.Clear();
    }

    private void MenuItemExit_Click(object sender, EventArgs e)
    {
        // TODO: close all apps
        Close();
    }

    private void MenuItemEmbed_Click(object sender, EventArgs e)
    {
        var tabPage = new TabPage($"Notepad2 [{Thread.CurrentThread.ManagedThreadId}]");
        var externalApp = new ExternalAppHost
        {
            Dock = DockStyle.Fill,
            Parent = tabPage,
        };
        externalApp.ApplicationStarted += ExternalApp_ApplicationStarted;
        externalApp.ApplicationClosed += ExternalApp_ApplicationClosed;

        TabControl.TabPages.Add(tabPage);

        externalApp.Start(new ExternalAppConfiguration
        {
            Command = @"C:\Program Files\Notepad2\Notepad2.exe",
        });
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

        TabControl.TabPages.Remove(tabPage);
    }
}