# AutoDI

Performs automatic dependency injection registration.
This allows you to have new types you create or add as libraries registered fully automatically
without having to edit your main project file every time.

## Installation

Simply install from nuget or reference the DLL directly.
You can find the plain DLL on each release on github.

### Signature Check

As of Version 1.1.0, the DLL file as well as the nuget are properly signed.
Check the signature before using the DLL or nuget.

## Quick Setup

Find the code where services are registered
(traditionally in `Startup.cs` but in more modern Projects also `Program.cs`)
and add `using AyrA.AutoDI;` to the top of the file.
Then insert this line in the location where you want your services registered:

```C#
builder.Services.AutoRegisterCurrentAssembly();
```

"builder" is your IHostBuilder instance. You may have named this differently.

See further below on how to set up a type for automated registration.

## Different Assembly

If you want to load additional types from different assemblies
you can specify the assembly:

```C#
builder.Services.AutoRegisterFromAssembly(assembly);
```

Note: This will not load types recursively.
It only checks the specified assembly but not referenced assemblies from it.

## Loading From All Assemblies

```C#
builder.Services.AutoRegisterAllAssemblies();
```

This will have AutoDI scan all loaded **and referenced** assemblies.
On large projects, this can take a few seconds,
especially if the debug logger is active (see bottom of this document).

By default, assemblies starting with these names are excluded:

- `Microsoft.`
- `System.`
- `AyrA.AutoDI`

You can change the exclusion list (see "Configuration" below)

## Optional "throwOnNoneType" argument

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

### Multiple registrations

You can apply the `AutoDIRegisterAttribute` multiple times to the same type to register it multiple times.
This permits you to register a given type with multiple interface types for example.

```C#
[AutoDIRegister(RegistrationType.Transient, typeof(ISomething))] //Register as ISomething
[AutoDIRegister(RegistrationType.Transient)] //Also register as itself
class Something : ISomething {/*...*/}
```

The registration type can also vary between attributes.
The order of the attributes is not relevant.

## Configuration

The `AutoDIExtensions` type has a few static properties you can use to configure it.
For them to have any effect, you need to set them before using one of the auto register functions.

### Boolean: DebugLogging

Default: `false` 

You can enable this flag to make AutoDI dump loading information to a logger and debug listeners.
This is disabled by default because it potentially generates a lot of messages.

Note: Output to debug listener is not working in the nuget package or the DLL from the GitHub releses section.
Those are compiled in release mode, which removes calls to the debug writer.

### TextWriter: Logger

Default: `System.Console.Error`

AutoDI doesn't uses the common "ILogger" logging system,
because AutoDI is used during early startup where a logging system is likely not yet set up.
You can set `AutoDIExtensions.Logger` to a custom logger that implements the TextWriter interface
such as `File.CreateText("...")` if you want to dump messages to a file.
By default it's assigned to the error stream of the console window.

If you just want to output to debug listeners, set `Logger = TextWriter.Null;`

### List<string>: NameExclusions

Default: `{"AyrA.AutoDI", "Microsoft.", "System."}`

This is a filter for the `AutoRegisterAllAssemblies` function.
This function would otherwise scan the entire .NET framework assembly tree for types with AutoDI attributes.
Because of this, assemblies starting with "Microsoft" or "System" are blacklisted by default.

This is a simple prefix string match, and is case sensitive.
It only has an effect on the mentioned function and will not prevent you from manually loading
a blacklisted assembly using the `AutoRegisterFromAssembly` function.
