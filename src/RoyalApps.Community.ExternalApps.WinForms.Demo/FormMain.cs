using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using RoyalApps.Community.ExternalApps.WinForms.Demo.Extensions;
using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using RoyalApps.Community.ExternalApps.WinForms.Events;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using RoyalApps.Community.ExternalApps.WinForms.Selection;

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
            var text = Program.ConsoleOutput.Snapshot();
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

        if (externalAppHost.Options == null)
            return;

        _logger.LogInformation("Starting application: {Command}", externalAppHost.Options.Launch.Executable);
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
        AddApplication(TabControlLeft, new ExternalAppOptions
        {
            Launch =
            {
                Executable = StripTextBoxQuickEmbed.Text,
                Arguments = string.IsNullOrWhiteSpace(StripTextBoxArguments.Text) ? null : StripTextBoxArguments.Text,
                WorkingDirectory = string.IsNullOrWhiteSpace(StripTextBoxWorkingDirectory.Text) ? null : StripTextBoxWorkingDirectory.Text,
            },
        });
    }

    private void StripButtonAddRight_Click(object sender, EventArgs e)
    {
        AddApplication(TabControlRight, new ExternalAppOptions
        {
            Launch =
            {
                Executable = StripTextBoxQuickEmbed.Text,
                Arguments = string.IsNullOrWhiteSpace(StripTextBoxArguments.Text) ? null : StripTextBoxArguments.Text,
                WorkingDirectory = string.IsNullOrWhiteSpace(StripTextBoxWorkingDirectory.Text) ? null : StripTextBoxWorkingDirectory.Text,
            },
        });
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

    private void AddApplication(TabControl tabControl, ExternalAppOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Launch.Executable))
        {
            _logger.LogWarning("No executable specified");
            return;
        }

        var expandedExecutable = Environment.ExpandEnvironmentVariables(options.Launch.Executable);
        var hasDirectoryComponent = expandedExecutable.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0;
        if (hasDirectoryComponent && !File.Exists(expandedExecutable))
        {
            _logger.LogError("Executable not found: {Command}", expandedExecutable);
            return;
        }

        Program.ConsoleOutput.Clear();

        options.Embedding.Mode = MenuItemControl.Checked
            ? EmbedMethod.Control
            : EmbedMethod.Window;
        options.Embedding.StartEmbedded = MenuItemStartEmbedded.Checked;
        options.Embedding.IncludeWindowChromeDimensions = MenuItemIncludeWindowChromeDimensions.Checked;
        options.Launch.StartHidden = MenuItemStartHidden.Checked;
        options.Launch.KillOnClose = MenuItemKillOnClose.Checked;

        var caption = Path.GetFileName(expandedExecutable);

        var tabPage = new TabPage(caption);
        var externalApp = new ExternalAppHost
        {
            Dock = DockStyle.Fill,
            LoggerFactory = _loggerFactory,
            Parent = tabPage,
        };
        externalApp.ApplicationStarted += ExternalApp_ApplicationStarted;
        externalApp.ApplicationClosed += ExternalApp_ApplicationClosed;
        externalApp.WindowSelectionRequested += ExternalApp_WindowSelectionRequested;

        tabControl.TabPages.Add(tabPage);
        tabControl.SelectedTab = tabPage;

        _logger.LogInformation("Starting application: {Command}", options.Launch.Executable);
        externalApp.Start(options);
    }

    private void ExternalApp_WindowSelectionRequested(object? sender, WindowSelectionRequestEventArgs e)
    {
        var topLevelCandidates = e.Candidates
            .Where(candidate => candidate.IsTopLevel)
            .ToList();
        var newlyDiscoveredTopLevelCandidates = e.NewlyDiscoveredCandidates
            .Where(candidate => candidate.IsTopLevel)
            .ToList();

        ExternalWindowCandidate? selectedCandidate = null;
        if (e.StartedProcessId is int startedProcessId)
        {
            selectedCandidate = newlyDiscoveredTopLevelCandidates.FirstOrDefault(candidate => candidate.ProcessId == startedProcessId)
                ?? topLevelCandidates.FirstOrDefault(candidate => candidate.ProcessId == startedProcessId);
        }

        if (selectedCandidate == null && !string.IsNullOrWhiteSpace(e.RequestedExecutablePath))
        {
            var requestedFileName = Path.GetFileName(e.RequestedExecutablePath);
            if (!string.IsNullOrWhiteSpace(requestedFileName))
            {
                selectedCandidate = newlyDiscoveredTopLevelCandidates.FirstOrDefault(candidate =>
                                       string.Equals(Path.GetFileName(candidate.ExecutablePath), requestedFileName, StringComparison.OrdinalIgnoreCase))
                                   ?? topLevelCandidates.FirstOrDefault(candidate =>
                                       string.Equals(Path.GetFileName(candidate.ExecutablePath), requestedFileName, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (selectedCandidate == null && newlyDiscoveredTopLevelCandidates.Count == 1)
            selectedCandidate = newlyDiscoveredTopLevelCandidates[0];

        if (selectedCandidate == null && topLevelCandidates.Count == 1)
            selectedCandidate = topLevelCandidates[0];

        if (selectedCandidate is { PrefersExternalHosting: true })
        {
            _logger.LogWarning(
                "Selected window '{WindowTitle}' may not reparent cleanly. {Warning}",
                selectedCandidate.WindowTitle,
                selectedCandidate.EmbeddingCompatibilityWarning);
        }

        if (selectedCandidate != null)
        {
            _logger.LogDebug(
                "Selecting candidate '{WindowTitle}' (pid {ProcessId}, hwnd {WindowHandle}) from {CandidateCount} candidates, {NewCandidateCount} newly discovered",
                selectedCandidate.WindowTitle,
                selectedCandidate.ProcessId,
                selectedCandidate.WindowHandle,
                topLevelCandidates.Count,
                newlyDiscoveredTopLevelCandidates.Count);
            e.SelectWindow(selectedCandidate);
        }
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
