# `WindowSelectionRequestEventArgs`

Provides the currently available candidate windows and allows the host to choose one.


## Type

```csharp
RoyalApps.Community.ExternalApps.WinForms.Events.WindowSelectionRequestEventArgs
```

## Properties

### `Candidates`

Gets the current candidate windows.


### `Elapsed`

Gets the time elapsed since selection started.


### `NewlyDiscoveredCandidates`

Gets the candidates that appeared for the first time during the current selection session.


### `RequestedExecutablePath`

Gets the executable path requested by the session, if one was configured.


### `SelectedWindowHandle`

Gets the handle of the selected window, or `System.IntPtr.Zero` when no window has been selected yet.


### `StartedProcessId`

Gets the identifier of the process started by the session, if one was launched.


### `Timeout`

Gets the maximum time allowed for selection.


## Methods

### `Constructor(Collections.Generic.IReadOnlyList<Selection.ExternalWindowCandidate>, TimeSpan, TimeSpan, Nullable<Int32>, String, Collections.Generic.IReadOnlyList<Selection.ExternalWindowCandidate>)`

Initializes a new instance of the `RoyalApps.Community.ExternalApps.WinForms.Events.WindowSelectionRequestEventArgs` class.


**Parameters**

- `candidates`: The currently available windows that may be selected for embedding.
- `elapsed`: The elapsed selection time.
- `timeout`: The configured selection timeout.
- `startedProcessId`: The identifier of the process started by the session, if a new process was launched.
- `requestedExecutablePath`: The executable path requested by the session, if one was configured.
- `newlyDiscoveredCandidates`: The subset of candidates that appeared for the first time during the current selection session.

### `SelectWindow(Selection.ExternalWindowCandidate)`

Selects a candidate window from the current candidate list.


**Parameters**

- `candidate`: The candidate to embed.

### `SelectWindow(IntPtr)`

Selects a candidate window by handle.


**Parameters**

- `windowHandle`: The handle of the window to embed.

[Back to API index](../index.md)
