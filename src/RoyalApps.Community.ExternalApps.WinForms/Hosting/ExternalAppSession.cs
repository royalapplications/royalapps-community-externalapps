using RoyalApps.Community.ExternalApps.WinForms.Discovery;
using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using RoyalApps.Community.ExternalApps.WinForms.Events;
using RoyalApps.Community.ExternalApps.WinForms.Interfaces;
using RoyalApps.Community.ExternalApps.WinForms.Launching;
using RoyalApps.Community.ExternalApps.WinForms.Options;
using RoyalApps.Community.ExternalApps.WinForms.Selection;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.Hosting;

internal sealed class ExternalAppSession : IDisposable
{
    private readonly ILogger<ExternalAppSession> _logger;
    private readonly IProcessLauncher _processLauncher;
    private readonly ISelectionSession _selectionSession;
    private readonly IEmbeddingController _embeddingController;
    private ExternalWindowCandidate? _selectedCandidate;
    private bool _windowRegistered;

    public ExternalAppSession(ExternalAppOptions options, ILoggerFactory loggerFactory)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<ExternalAppSession>();
        _processLauncher = new ProcessLauncher(loggerFactory.CreateLogger<ProcessLauncher>());
        var processMetadataProvider = new ProcessMetadataProvider(loggerFactory.CreateLogger<ProcessMetadataProvider>());
        var windowCatalog = new WindowCatalog(
            loggerFactory.CreateLogger<WindowCatalog>(),
            processMetadataProvider,
            new ExternalWindowCandidateFactory(new EmbeddingCompatibilityAssessor()));
        _selectionSession = new SelectionSession(windowCatalog, loggerFactory.CreateLogger<SelectionSession>());
        _embeddingController = new EmbeddingController(loggerFactory.CreateLogger<EmbeddingController>());
    }

    internal ExternalAppSession(
        ExternalAppOptions options,
        ILoggerFactory loggerFactory,
        IProcessLauncher processLauncher,
        ISelectionSession selectionSession,
        IEmbeddingController embeddingController)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
        _selectionSession = selectionSession ?? throw new ArgumentNullException(nameof(selectionSession));
        _embeddingController = embeddingController ?? throw new ArgumentNullException(nameof(embeddingController));
        _logger = loggerFactory.CreateLogger<ExternalAppSession>();
    }

    public event EventHandler? ProcessExited;

    public ApplicationState ApplicationState { get; private set; } = ApplicationState.Stopped;

    public ExternalAppOptions Options { get; }

    public Process? Process { get; private set; }

    public HWND WindowHandle { get; private set; }

    public bool HasWindow => WindowHandle != HWND.Null;

    public bool IsRunning => Process is { HasExited: false };

    public AttachmentState AttachmentState { get; private set; } = AttachmentState.None;

    public bool IsEmbedded => AttachmentState == AttachmentState.Embedded;

    public bool IsExternal => AttachmentState == AttachmentState.External;

    public async Task StartAsync(Action<WindowSelectionRequestEventArgs>? raiseSelectionRequest, CancellationToken cancellationToken)
    {
        if (ApplicationState != ApplicationState.Stopped)
            throw new InvalidOperationException("Cannot start an external app session that is already running.");

        ApplicationState = ApplicationState.Starting;

        try
        {
            var baselineWindowHandles = _selectionSession.CaptureKnownWindowHandles();
            var launchResult = await _processLauncher.StartAsync(Options, cancellationToken);
            Process = launchResult.Process;

            var selectionResult = await _selectionSession.SelectWindowAsync(
                launchResult.Process,
                Options.Launch.Executable,
                baselineWindowHandles,
                Options.Selection,
                raiseSelectionRequest,
                cancellationToken);

            ApplySelectionResult(selectionResult);

            if (Process != null)
            {
                Process.EnableRaisingEvents = true;
                Process.Exited += AppProcess_Exited;
                ExternalApps.TrackProcess(Process, _logger);
            }

            ApplicationState = ApplicationState.Running;
        }
        catch
        {
            ApplicationState = ApplicationState.Stopped;
            throw;
        }
    }

    public void CloseApplication()
    {
        if (Process is not { HasExited: false })
            return;

        try
        {
            Process.Exited -= AppProcess_Exited;

            if (Options.Launch.KillOnClose || !Process.CloseMainWindow())
                Process.Kill();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Closing external application failed");
        }
        finally
        {
            ApplicationState = ApplicationState.Stopped;
            AttachmentState = AttachmentState.None;
        }
    }

    public async Task EmbedAsync(ExternalAppHost ownerControl, CancellationToken cancellationToken)
    {
        if (!HasWindow)
            throw new MissingWindowException();

        LogEmbeddingCompatibilityWarningIfNeeded();
        await _embeddingController.EmbedAsync(ownerControl, WindowHandle, Options.Embedding, cancellationToken);
        AttachmentState = AttachmentState.Embedded;
    }

    public void DetachApplication()
    {
        if (!HasWindow)
            return;

        _embeddingController.Detach(WindowHandle);
        AttachmentState = AttachmentState.Detached;
    }

    public void FocusApplication()
    {
        if (!HasWindow)
            return;

        _embeddingController.Focus(WindowHandle);
    }

    public void SetWindowPosition(int x, int y, int width, int height)
    {
        if (!HasWindow)
            return;

        _embeddingController.SetWindowPosition(WindowHandle, new Rectangle(x, y, width, height), Options.Embedding);
    }

    public string GetWindowTitle()
    {
        return !HasWindow
            ? string.Empty
            : NativeWindowUtilities.GetWindowTitle(WindowHandle);
    }

    public void Dispose()
    {
        if (Process != null)
            Process.Exited -= AppProcess_Exited;

        UnregisterTrackedWindow();
        _embeddingController.Dispose();
        AttachmentState = AttachmentState.None;
    }

    public void MarkAsExternal()
    {
        if (!HasWindow)
        {
            AttachmentState = AttachmentState.None;
            return;
        }

        AttachmentState = AttachmentState.External;
    }

    private void AppProcess_Exited(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Process process)
                process.Exited -= AppProcess_Exited;

            ApplicationState = ApplicationState.Stopped;
            AttachmentState = AttachmentState.None;
            ProcessExited?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProcessExited event threw an exception");
        }
    }

    private void ApplySelectionResult(WindowSelectionResult selectionResult)
    {
        ArgumentNullException.ThrowIfNull(selectionResult);

        switch (selectionResult.Outcome)
        {
            case WindowSelectionOutcome.Selected:
                var selectedCandidate = selectionResult.SelectedCandidate ?? throw new MissingWindowException();
                _selectedCandidate = selectedCandidate;
                WindowHandle = new HWND(selectedCandidate.WindowHandle);
                RegisterTrackedWindow();
                Process ??= Process.GetProcessById(selectedCandidate.ProcessId);
                AttachmentState = AttachmentState.External;
                break;
            case WindowSelectionOutcome.TimedOut when Process != null:
                AttachmentState = AttachmentState.None;
                break;
            case WindowSelectionOutcome.StartedProcessExited:
                throw new InvalidOperationException("The started process exited before a window could be selected.");
            default:
                throw new MissingWindowException();
        }
    }

    private void LogEmbeddingCompatibilityWarningIfNeeded()
    {
        if (_selectedCandidate is not { PrefersExternalHosting: true } selectedCandidate)
            return;

        if (Options.Embedding.Mode is not (EmbedMethod.Control or EmbedMethod.Window))
            return;

        _logger.LogWarning(
            "Selected window '{WindowTitle}' ({ProcessName}, class '{ClassName}') is a poor candidate for reparenting. {Warning}",
            selectedCandidate.WindowTitle,
            selectedCandidate.ProcessName,
            selectedCandidate.ClassName,
            selectedCandidate.EmbeddingCompatibilityWarning);
    }

    private void RegisterTrackedWindow()
    {
        if (_windowRegistered || WindowHandle == HWND.Null)
            return;

        ExternalApps.RegisterTrackedWindow(WindowHandle);
        _windowRegistered = true;
    }

    private void UnregisterTrackedWindow()
    {
        if (!_windowRegistered || WindowHandle == HWND.Null)
            return;

        ExternalApps.UnregisterTrackedWindow(WindowHandle);
        _windowRegistered = false;
    }
}
