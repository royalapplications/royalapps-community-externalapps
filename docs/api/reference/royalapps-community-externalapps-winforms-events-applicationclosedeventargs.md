# `ApplicationClosedEventArgs`

Event arguments for the ApplicationClosed event.


## Type

```csharp
RoyalApps.Community.ExternalApps.WinForms.Events.ApplicationClosedEventArgs
```

## Properties

### `Exception`

The exception which may have caused the application to close.


### `ProcessExited`

True, if the process exited.


### `UserInitiated`

True, if the user has closed the application using the CloseApplication() method, otherwise false.


## Methods

### `Constructor()`

Initializes a new instance of the `RoyalApps.Community.ExternalApps.WinForms.Events.ApplicationClosedEventArgs` class.


### `Constructor(Exception)`

Initializes a new instance of the `RoyalApps.Community.ExternalApps.WinForms.Events.ApplicationClosedEventArgs` class and sets the `RoyalApps.Community.ExternalApps.WinForms.Events.ApplicationClosedEventArgs.Exception` property.


**Parameters**

- `exception`: The exception that caused the session to close.

[Back to API index](../index.md)
