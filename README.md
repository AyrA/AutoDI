# AutoDI

Performs automatic dependency injection registration.
This allows you to have new types you create or add as libraries registered fully automatically without having to edit your main project file every time.

## Installation

Simply install from nuget or reference the DLL directly.
You can find the plain DLL on each release on github.

### Signature Check

As of Version 1.1.0, the DLL file as well as the nuget are properly signed.
Check the signature before using the DLL or nuget.

## Quick Setup

Find the code where services are registered
(traditionally in `Startup.cs` but in more modern projects also `Program.cs`)
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

The values correspond to the appropriate function you would use to manually register types in your project startup routine.

The enumeration also has a `Custom` value, which cannot be used in this context.

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

### Custom registration function

The AutoDIRegister attribute contains a constructor with a single string argument.
This is the name of a custom registration function you want to be called instead of the default logic being used.

The function must be declared in the type you use the AutoDIRegister attribute on, and it must be static.
You may declare it as non-public to avoid manual calls to the function.

**Example**:

```C#
[AutoDIRegister(nameof(Register))] //Register using custom function
class Something
{
	private static void Register(IServiceCollection services, AutoDIRegisterAttribute attr)
	{
		//Custom logic goes here
	}
}
```

Your registration function must at least take the IServiceCollection argument,
and may optionally take the AutoDIRegisterAttribute argument.
The return type of the function is not relevant.
When AutoDI searches for your function, it prefers signatures with both arguments first,
and it prefers public over non-public methods.

**Note**: AutoDI does in no way enforce the registration function to actually register its contained type.
You may register as many types as you want, or perform no registration at all.

If multiple attributes with the same registration function are supplied,
the function will be individually called for every attribute that references it.

*I'm aware that using a string argument for the function name is ugly,
but it's a limitation of the CLR at the moment (see CS0181) that you cannot use generic types or delegates. You can use `nameof(...)` to avoid a hardcoded string*

### Default Registration

Using the AutoDIRegister attribute without any arguments
has the same effect as using `services.Add(Transient|Scoped|Singleton)<T>()`

### Hosted Services

Apply the `AutoDIHostedServiceAttribute` attribute to a class to have it loaded as a hosted service.
For this to work, the class must implement the `Microsoft.Extensions.Hosting.IHostedService` interface.

#### Add Singleton

Apply the `AutoDIHostedSingletonServiceAttribute` attribute to a class to have it loaded as a hosted service.
For this to work, the class must implement the `Microsoft.Extensions.Hosting.IHostedService` interface.
This attribute will additionally register the instance as a singleton service.
The singleton and hosted service will be the same instance.

## Registration Filtering

All `AutoDI*` attributes provide a `Filters` property you can set.
The property takes a comma separated list of strings.
The list is case insensitive, and leading or trailing whitespace is stripped.

If the list is not empty, the registration is only performed
if at least one item in the list matches the list from `AutoDIExtensions.Filters`,
otherwise the type will be skipped.

Entries may optionally be prefixed with `!` to convert it into an exclusion.
If at least one exclusion matches the type will not be loaded.
Exclusions take precedence over inclusions.
For example `Test,!Test` will never be loaded
regardless of whether the `Test` filter is present or not,
because whenever the inclusion matches, the exclusion matches too.

## Configuration

The `AutoDIExtensions` type has a few static properties you can use to configure it.
For them to have any effect, you need to set them before using one of the auto register functions.

### Boolean: DebugLogging

Default: `false` 

You can enable this flag to make AutoDI dump loading information to a logger and debug listeners.
This is disabled by default because it potentially generates a lot of messages.

Note: Output to debug listener is not working in the nuget package or the DLL from the GitHub releses section.
Those are compiled in release mode, which removes calls to the debug writer.
Using the `Logger` property still works.

### TextWriter: Logger

Default: `System.Console.Error`

AutoDI doesn't uses the common "ILogger" logging system,
because AutoDI is used during early startup where a logging system is likely not yet set up.
You can set `AutoDIExtensions.Logger` to a custom logger that implements the TextWriter interface
such as `File.CreateText("...")` if you want to dump messages to a file.
By default it's assigned to the error stream of the console window.

If you just want to output to debug listeners, set `Logger = TextWriter.Null;`

Regardless of the value assigned to this property,
log messages are suppressed if `DebugLogging` is `false`

### List<string>: NameExclusions

Default: `{"AyrA.AutoDI", "Microsoft.", "System."}`

This is a filter for the `AutoRegisterAllAssemblies` function.
This function would otherwise scan the entire .NET framework assembly tree for types with AutoDI attributes.
Because of this, assemblies starting with "Microsoft" or "System" are blacklisted by default.

This is a simple prefix string match, and is case sensitive.
It only has an effect on the mentioned function and will not prevent you from manually loading
a blacklisted assembly using the `AutoRegisterFromAssembly` function.

### List<string>: FilterList

Default: empty

If empty, only types with an empty filter list are loaded.
If not empty, the filter list of types to be loaded is compared against this list.

If the type list is empty, the type is loaded unconditionally.
If at least one exclusion matches, then the type is not loaded.
If at least one inclusion matches while no exclusion matches, the type is loaded.
