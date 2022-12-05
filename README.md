# AutoDI

Performs automatic dependency injection registration.
This allows you to have new types you create or add as libraries registered fully automatically.

## Installation

Simply install from nuget or reference the DLL directly.

## Quick Setup

Find the code where services are registered
(traditionally in `Startup.cs` but in more modern Projects also `Program.cs`)
and insert this line:

```C#
builder.Services.AutoRegisterCurrentAssembly();
```

See further below on how to set up a type for automated registration.

## Advanced setup

If you want to load additional types from different assemblies
you can specify the assembly:

```C#
builder.Services.AutoRegisterFromAssembly(assembly);
```

Note: This will not load types recursively.
It only checks the specified assembly but not referenced assemblies from it.

You can also use `builder.Services.AutoRegisterAllAssemblies();`
to have AutoDI scan all loaded assemblies.
On large projects, this can take a few seconds,
especially if the debug logger is active (see bottom of this document)

### Optional "throwOnNoneType" argument

All loader functions have an optional boolean argument named "throwOnNoneType".
It defaults to "false".

The `AutoDIAttribute` contains a value "None" which can be used to specify a type you do not want to load.
Reasons for this may vary, but among other things,
you can use this value to load a type only in debug builds or release builds
by making it conditional using `#if DEBUG` directives.

Setting this optional parameter to "true" will make AutoDI throw an exception if the "None" value is encountered.
This can be used to assert that there are no "None" types in a release build for example.

## Setting up a type for automatic registration

Simply add the `AutoDIRegisterAttribute` as shown below to the classes you want to automatically register:

```C#
[AutoDIRegister(RegistrationType.Transient)]
class Something {/*...*/}
```

Replace `RegistrationType.Transient` with the appropriate type:

- Transient
- Singleton
- Scoped
- None

The values correspond to the appropriate function you would use to manually register types in your project startup routine.
"None" is special and essentially behaves as if the attribute was not there at all.

### Interfaces

Under some circumstances you do not want to register an AutoDI type under its own type,
but rather an interface it implements.
To achieve this, you can add the interface type to the attribute declaration:

```C#
[AutoDIRegister(RegistrationType.Transient, typeof(ISomething))]
class Something : ISomething {/*...*/}
```

Note: AutoDI doesn't actually checks if the type implements the specified interface.

## Debugging

You can set `AutoDIExtensions.DebugLogging = true;` to make AutoDI dump loading information to a logger and debug listeners.
This is disabled by default because it potentially generates a lot of messages.

AutoDI doesn't uses the common "ILogger" logging system,
because AutoDI is used during early startup where a logging system is likely not set up.
You can set `AutoDIExtensions.Logger` to a custom logger that implements the TextWriter interface
such as `File.CreateText("...")` if you want to dump messages to a file.
By default it's assigned to the error stream of the console window.

If you just want to output to debug listeners, set `Logger = TextWriter.Null;`