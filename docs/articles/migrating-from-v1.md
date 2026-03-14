---
title: Migrate from v1
---

# Migrating from v1

Version `2.x` changes the public API substantially.

## Configuration model

`ExternalAppConfiguration` was removed and replaced by `ExternalAppOptions`.

- `Launch` contains startup and process-lifetime options.
- `Embedding` contains embedding mode, startup embedding behavior, and whether window chrome dimensions are compensated for during sizing.
- `Selection` contains timeout and polling behavior.

`Launch.Executable` can now be either a full path or a command name. `%VAR%` expansion is supported for both `Launch.Executable` and `Launch.WorkingDirectory`, and `Launch.EnvironmentVariables` can override values during expansion.

When `Launch.UseExistingProcess` is enabled, startup becomes discovery-only and skips process creation entirely.

## Window identification

Static matcher properties are no longer part of the library API.

- removed title matching
- removed class-name matching
- removed process-name matching
- removed command-line matching
- removed skip-count matching

Instead, handle `WindowSelectionRequested` and select a window from the current `ExternalWindowCandidate` list.

The selection event now also provides:

- `RequestedExecutablePath` to help correlate launcher stubs and process handoff scenarios
- `NewlyDiscoveredCandidates` to help prefer windows that appeared during the current start attempt

Candidate windows also expose compatibility hints:

- `PrefersExternalHosting`
- `EmbeddingCompatibilityWarning`

Those hints indicate that the selected window may be a poor candidate for Win32 reparenting and may need to remain external.

## Native dependency removal

The library no longer ships or depends on `WinEmbed.dll`. Full-window and client-area embedding are implemented in managed code with Win32 interop.

There is no dedicated `EmbedMethod.External` in v2. If a selected window cannot be reparented, the library keeps it external, logs a warning, and keeps the session alive.
