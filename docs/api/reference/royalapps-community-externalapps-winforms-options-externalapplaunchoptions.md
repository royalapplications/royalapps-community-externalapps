# `ExternalAppLaunchOptions`

Defines how the external process is launched or attached.


## Type

```csharp
RoyalApps.Community.ExternalApps.WinForms.Options.ExternalAppLaunchOptions
```

## Properties

### `Arguments`

Gets or sets the command-line arguments passed to the executable.


### `Credentials`

Gets the credential options used when starting the process under alternate credentials.


### `EnvironmentVariables`

Gets the environment variables applied to the started process when direct process creation is used.


Custom environment variables are not supported for elevated launches because elevation requires shell execution.


### `Executable`

Gets or sets the full path to the executable that should be started.


### `KillOnClose`

Gets or sets a value indicating whether the tracked process should be terminated if graceful close does not succeed.


### `RunElevated`

Gets or sets a value indicating whether the process should be launched elevated.


### `StartHidden`

Gets or sets a value indicating whether newly started processes should initially be hidden while selection runs.


### `UseExistingProcess`

Gets or sets a value indicating whether an already running process should be reused instead of launching a new one.


### `WorkingDirectory`

Gets or sets the working directory used when the process is started.


[Back to API index](../index.md)
