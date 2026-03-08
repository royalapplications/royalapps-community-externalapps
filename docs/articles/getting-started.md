---
title: Getting Started
---

# Getting Started

## Install the package

Install the library from NuGet before wiring it into your application.

```powershell
Install-Package RoyalApps.Community.ExternalApps.WinForms
```

```cmd
dotnet add package RoyalApps.Community.ExternalApps.WinForms
```

## Initialize the library

Call `ExternalApps.Initialize()` once when your application starts and `ExternalApps.Cleanup()` once during shutdown.

## Add the host control

Add `ExternalAppHost` to a form or container and size it like any other WinForms control.

## Start a session

Create `ExternalAppOptions` and call `Start(...)` on the host.

```csharp
using System;
using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using RoyalApps.Community.ExternalApps.WinForms.Options;

var options = new ExternalAppOptions
{
    Launch =
    {
        Executable = @"%windir%\system32\cmd.exe",
        KillOnClose = true,
    },
    Embedding =
    {
        Mode = EmbedMethod.Window,
        StartEmbedded = true,
        IncludeWindowChromeDimensions = true,
    },
    Selection =
    {
        Timeout = TimeSpan.FromSeconds(10),
        PollInterval = TimeSpan.FromMilliseconds(250),
    },
};

externalAppHost.Start(options);
```

`Embedding.IncludeWindowChromeDimensions` controls whether the embedded window's title bar and frame dimensions are included when sizing it to the host. Leave it enabled when you want the target app's client area to fill the host bounds. Disable it if the target window behaves better when the requested bounds are applied directly.

`Launch.Executable` can be either:

- a full path such as `C:\Windows\System32\notepad.exe`
- a command name such as `notepad.exe` or `pwsh`

When the library can use shell execution, command names are launched the same way they would be from Windows shell resolution. When direct process creation is required, for example because credentials or custom environment variables are configured, the library resolves the executable against `PATH`.

You can also provide custom environment variables:

```csharp
options.Launch.EnvironmentVariables["MY_TOOL_MODE"] = "demo";
```

`Launch.Executable` and `Launch.WorkingDirectory` support `%VAR%` expansion, and `Launch.EnvironmentVariables` participate in that expansion.

When `Launch.UseExistingProcess` is enabled, the library skips process creation and only performs candidate discovery against windows that already exist.

## Select the correct window

The library no longer auto-matches windows from configuration. Subscribe to `WindowSelectionRequested` and choose a window from the provided candidate list.

```csharp
using System.Linq;
using RoyalApps.Community.ExternalApps.WinForms.Embedding;
using RoyalApps.Community.ExternalApps.WinForms.Options;

externalAppHost.WindowSelectionRequested += (_, e) =>
{
    var candidate = e.NewlyDiscoveredCandidates
        .Concat(e.Candidates)
        .Where(window => window.IsVisible && window.IsTopLevel)
        .FirstOrDefault(window =>
            window.ProcessId == e.StartedProcessId ||
            string.Equals(
                System.IO.Path.GetFileName(window.ExecutablePath),
                System.IO.Path.GetFileName(e.RequestedExecutablePath),
                StringComparison.OrdinalIgnoreCase));

    if (candidate?.PrefersExternalHosting == true)
    {
        Console.WriteLine(candidate.EmbeddingCompatibilityWarning);
    }

    if (candidate is not null)
        e.SelectWindow(candidate);
};
```

The event is raised repeatedly until a window is selected or the configured timeout expires.

For modern or packaged desktop apps, inspect:

- `ExternalWindowCandidate.PrefersExternalHosting`
- `ExternalWindowCandidate.EmbeddingCompatibilityWarning`
- `WindowSelectionRequestEventArgs.RequestedExecutablePath`
- `WindowSelectionRequestEventArgs.NewlyDiscoveredCandidates`

When those are populated, the library is warning you that Win32 reparenting may be unstable and that the selected window may need to remain external.

If reparenting still fails after selection, the library leaves the window external, logs a warning, and keeps the session alive instead of treating startup as a fatal error.

There is no dedicated `EmbedMethod.External` mode. External fallback is automatic when selection succeeds but reparenting fails.

Use `ExternalAppHost.AttachmentState` to distinguish:

- `None`: no window is currently attached
- `External`: a window was selected but is currently left external
- `Detached`: a selected window was detached from the host
- `Embedded`: the selected window is currently embedded

## React to lifecycle events

- `ApplicationStarted` indicates startup and initial embedding completed.
- `ApplicationClosed` indicates the tracked process exited or startup failed.
- `ApplicationActivated` indicates the tracked window received focus.
- `WindowTitleChanged` indicates the tracked window caption changed.
- `WindowSelectionRequested` is raised until your code selects a candidate or the timeout elapses.
