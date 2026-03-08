# `EmbedMethod`

How to embed the external application window into the ExternalAppHost.


## Type

```csharp
RoyalApps.Community.ExternalApps.WinForms.Embedding.EmbedMethod
```

## Fields

### `Control`

Only the client area of the external app window is embedded (without the main menu).
The limitation of this method is that some applications may look like they are not focused/active.


### `Window`

The whole window is embedded including the main menu (if available).
The limitation of this method is that the ALT-TAB order may be incorrect.


[Back to API index](../index.md)
