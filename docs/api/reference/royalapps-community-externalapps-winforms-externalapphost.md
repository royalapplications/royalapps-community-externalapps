# `ExternalAppHost`

The host control which can embed external application windows.


## Type

```csharp
RoyalApps.Community.ExternalApps.WinForms.ExternalAppHost
```

## Properties

### `AttachmentState`

Gets how the current external window is attached to the host.


### `EmbeddedWindowHandle`

Gets the handle of the currently tracked external window, or `System.IntPtr.Zero` when no window is attached.


### `IsEmbedded`

Gets a value indicating whether the current window is embedded in the host control.


### `LoggerFactory`

Gets or sets the logger factory used to create host loggers.


### `Options`

Gets the options used by the current session, or `null` when no session is active.


### `Process`

Gets the tracked process for the current session, or `null` when no process is attached.


## Events

### `ApplicationActivated`

Occurs when the embedded application receives focus.


### `ApplicationClosed`

Occurs when the tracked application closes or the session terminates with an error.


### `ApplicationStarted`

Occurs after the application has started and any initial embedding work has completed.


### `WindowSelectionRequested`

Occurs while candidate windows are being discovered and allows the consumer to choose the window to embed.


### `WindowTitleChanged`

Occurs when the tracked window caption changes.


## Methods

### `CloseApplication()`

Requests that the tracked application is closed.


Depending on the application, this may show confirmation dialogs. If `RoyalApps.Community.ExternalApps.WinForms.Options.ExternalAppLaunchOptions.KillOnClose` is enabled,
the process is terminated when graceful shutdown fails.


### `DetachApplication()`

Detaches the tracked application window from the host control.


### `EmbedApplication()`

Re-embeds a previously detached application window.


### `FocusApplication(Boolean)`

Transfers focus to the tracked application.


**Parameters**

- `force`: `true` to focus even when the window is detached; otherwise, focus is applied only while embedded.

### `GetWindowScreenshot()`

Captures a bitmap of the currently tracked window.


Returns: A bitmap of the client area, or `null` when no valid window is available.

### `MaximizeApplication()`

Maximizes the tracked application window.


### `SetWindowPosition()`

Repositions the tracked application window to match the current host bounds.


### `SetWindowPosition(Drawing.Rectangle)`

Moves and resizes the tracked application window to the specified bounds.


**Parameters**

- `rectangle`: The target bounds, in host coordinates when embedded or screen coordinates when detached.

### `ShowSystemMenu(Drawing.Point)`

Displays the system menu of the tracked window at the specified control-relative location.


**Parameters**

- `location`: The location, in control coordinates, where the menu should be shown.

### `Start(Options.ExternalAppOptions)`

Starts a new external application session with the specified options.


**Parameters**

- `options`: The runtime options that control launch, selection, and embedding behavior.

This method schedules the startup workflow on a background task and returns immediately. Subscribe to
`RoyalApps.Community.ExternalApps.WinForms.ExternalAppHost.ApplicationStarted`, `RoyalApps.Community.ExternalApps.WinForms.ExternalAppHost.ApplicationClosed`, and `RoyalApps.Community.ExternalApps.WinForms.ExternalAppHost.WindowSelectionRequested` to observe progress.


[Back to API index](../index.md)
