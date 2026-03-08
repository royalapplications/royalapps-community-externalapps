using System;
using RoyalApps.Community.ExternalApps.WinForms.Embedding;

namespace RoyalApps.Community.ExternalApps.WinForms.Selection;

/// <summary>
/// Describes a candidate window that can be embedded by the host.
/// </summary>
public sealed class ExternalWindowCandidate
{
    /// <summary>
    /// Gets the native handle of the candidate window.
    /// </summary>
    public required IntPtr WindowHandle { get; init; }

    /// <summary>
    /// Gets the process identifier that owns the candidate window.
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Gets the process name that owns the candidate window.
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Gets the full executable path for the owning process when available.
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Gets the full command line for the owning process when available.
    /// </summary>
    public required string CommandLine { get; init; }

    /// <summary>
    /// Gets the native window class name.
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Gets the current window caption.
    /// </summary>
    public required string WindowTitle { get; init; }

    /// <summary>
    /// Gets a value indicating whether the candidate window is visible.
    /// </summary>
    public bool IsVisible { get; init; }

    /// <summary>
    /// Gets a value indicating whether the candidate window is a top-level window.
    /// </summary>
    public bool IsTopLevel { get; init; }

    /// <summary>
    /// Gets a value indicating whether the candidate looks like it should remain a top-level window instead of being reparented.
    /// </summary>
    public bool PrefersExternalHosting { get; init; }

    /// <summary>
    /// Gets a warning that explains why the candidate may be risky to embed with <see cref="EmbedMethod.Control"/> or <see cref="EmbedMethod.Window"/>.
    /// </summary>
    public string EmbeddingCompatibilityWarning { get; init; } = string.Empty;
}
