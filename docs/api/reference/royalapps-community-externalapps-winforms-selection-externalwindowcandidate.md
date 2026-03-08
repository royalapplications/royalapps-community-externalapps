# `ExternalWindowCandidate`

Describes a candidate window that can be embedded by the host.


## Type

```csharp
RoyalApps.Community.ExternalApps.WinForms.Selection.ExternalWindowCandidate
```

## Properties

### `ClassName`

Gets the native window class name.


### `CommandLine`

Gets the full command line for the owning process when available.


### `EmbeddingCompatibilityWarning`

Gets a warning that explains why the candidate may be risky to embed with `RoyalApps.Community.ExternalApps.WinForms.Embedding.EmbedMethod.Control` or `RoyalApps.Community.ExternalApps.WinForms.Embedding.EmbedMethod.Window`.


### `ExecutablePath`

Gets the full executable path for the owning process when available.


### `IsTopLevel`

Gets a value indicating whether the candidate window is a top-level window.


### `IsVisible`

Gets a value indicating whether the candidate window is visible.


### `PrefersExternalHosting`

Gets a value indicating whether the candidate looks like it should remain a top-level window instead of being reparented.


### `ProcessId`

Gets the process identifier that owns the candidate window.


### `ProcessName`

Gets the process name that owns the candidate window.


### `WindowHandle`

Gets the native handle of the candidate window.


### `WindowTitle`

Gets the current window caption.


[Back to API index](../index.md)
