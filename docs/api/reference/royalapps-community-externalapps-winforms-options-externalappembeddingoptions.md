# `ExternalAppEmbeddingOptions`

Defines how a resolved window is embedded.


## Type

```csharp
RoyalApps.Community.ExternalApps.WinForms.Options.ExternalAppEmbeddingOptions
```

## Properties

### `IncludeWindowChromeDimensions`

Gets or sets a value indicating whether the embedded window's non-client area, such as the title bar and frame, should be accounted for when sizing it to the host bounds.


When enabled, the library expands the target rectangle so the embedded window's client area fills the requested bounds.
When disabled, the requested bounds are applied directly to the embedded window.


### `Mode`

Gets or sets the embedding mode used for the selected window.


### `StartEmbedded`

Gets or sets a value indicating whether the selected window should be embedded immediately after startup.


[Back to API index](../index.md)
