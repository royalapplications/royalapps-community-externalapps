---
title: Selection Strategies
---

# Selection Strategies

Version `2.x` no longer selects windows from static title, class, or process-name configuration. Instead, the host raises `WindowSelectionRequested` repeatedly while candidate discovery is running, and your code decides which `ExternalWindowCandidate` to attach.

This page covers the patterns that work best in practice.

## Start with the newest candidates

When multiple instances of the same application may already be open, prefer `NewlyDiscoveredCandidates` before falling back to the full `Candidates` list.

```csharp
externalAppHost.WindowSelectionRequested += (_, e) =>
{
    var candidate = e.NewlyDiscoveredCandidates
        .Concat(e.Candidates)
        .FirstOrDefault(window => window.IsVisible && window.IsTopLevel);

    if (candidate is not null)
        e.SelectWindow(candidate);
};
```

`NewlyDiscoveredCandidates` contains windows that appeared for the first time during the current selection session. That makes it the best first filter when a second or third instance of the same app is launched.

## Handle launcher stubs and process handoff

Some applications start through a short-lived launcher process and then show their real window in another process. Current Notepad on Windows 11 is a common example.

In those cases:

- `StartedProcessId` may not match the eventual window process
- `RequestedExecutablePath` remains useful as the original launch target
- `NewlyDiscoveredCandidates` helps distinguish the new top-level window from older instances

Use all three signals together:

```csharp
externalAppHost.WindowSelectionRequested += (_, e) =>
{
    var requestedFileName = Path.GetFileName(e.RequestedExecutablePath);

    var candidate = e.NewlyDiscoveredCandidates
        .Concat(e.Candidates)
        .Where(window => window.IsVisible && window.IsTopLevel)
        .FirstOrDefault(window =>
            window.ProcessId == e.StartedProcessId ||
            string.Equals(
                Path.GetFileName(window.ExecutablePath),
                requestedFileName,
                StringComparison.OrdinalIgnoreCase));

    if (candidate is not null)
        e.SelectWindow(candidate);
};
```

## Reuse an existing process deliberately

When `Launch.UseExistingProcess` is enabled, the library does not create a new process. Discovery runs only against windows that already exist.

That means your selection code should rely on stable characteristics of the target window, for example:

- executable path
- process name
- class name
- window title
- command line

This is the mode where application-specific matching logic belongs.

## Watch for compatibility hints

Some windows look like poor candidates for Win32 reparenting. The library exposes heuristics for those cases on each candidate:

- `PrefersExternalHosting`
- `EmbeddingCompatibilityWarning`

If those values are populated, the window may still embed successfully, but `EmbedMethod.Control` and `EmbedMethod.Window` are more likely to behave poorly.

```csharp
if (candidate?.PrefersExternalHosting == true)
{
    logger.LogWarning(
        "Selected window may not reparent cleanly: {Warning}",
        candidate.EmbeddingCompatibilityWarning);
}
```

## Understand timeout and fallback behavior

The selection loop continues until one of these things happens:

- your code calls `SelectWindow(...)`
- the configured timeout elapses
- the selection operation is canceled

If the timeout elapses without a selected window:

- the started process is left running externally
- the session has no selected window
- `ExternalAppHost.AttachmentState` stays `None`

If a window is selected but embedding later fails:

- the session keeps the selected window
- the window remains external instead of being embedded
- `ExternalAppHost.AttachmentState` becomes `External`

If a window is successfully embedded:

- `ExternalAppHost.AttachmentState` becomes `Embedded`

If an embedded window is later detached:

- `ExternalAppHost.AttachmentState` becomes `Detached`

## Recommended selection order

For most consumers, this order works well:

1. `NewlyDiscoveredCandidates` with `StartedProcessId`
2. `Candidates` with `StartedProcessId`
3. `NewlyDiscoveredCandidates` with `RequestedExecutablePath`
4. `Candidates` with `RequestedExecutablePath`
5. Application-specific fallback rules such as class name or title

That sequence handles:

- multiple already-running instances
- launcher-stub handoff
- delayed top-level window creation
- packaged or modern desktop apps that move the window to another process
