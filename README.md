# AutoDI

Performs automatic dependency injection registration.
This allows you to have new types you create or add as libraries registered fully automatically.

## Installation

Install from nuget, then set up your application for automatic DI.
Usually this means finding the code where services are registered
(traditionally in `Startup.cs` but in more modern Projects also `Program.cs`)
and inserting this line:

```C#
builder.Services.AutoRegisterAll();
```

The line can be repeated with arguments
if you want to load additional types from different assemblies.

You can also use `builder.Services.AutoRegisterAllAssemblies();`
to have AutoDI scan all loaded assemblies.

### Custom arguments

You can specify up to two arguments for the function,
each one is optional, and each one can also be used on its own.

#### Assembly assembly

This argument tells AutoDI from where to load automated types from.
By default, it loads from the assembly that calls `AutoRegisterAll()`,
which almost always means it loads them from your main project.
You can specify the assembly if you want to load AutoDI types from a different assembly,
such as a referenced DLL file for example.

#### bool throwOnNoneType

The AutoDIAttribute contains a value "None" which can be used to specify a type you do not want to load.
Reasons for this may vary, but among other things,
you can use this value to load a type only in debug builds or release builds.

Setting this parameter to true will make AutoDI throw an exception if the "None" value is encountered.
This can be used to assert that there are no "None" types in a release build for example.

## Setting up a type for automatic registration

Simply add the attribute below to the class you want to automatically register:

```C#
[AutoRegister(RegistrationType.None)]
class Something {/*...*/}
```

Replace `RegistrationType.None` with the appropriate type:

- Transient
- Singleton
- Scoped

The values correspond to the appropriate function you would use to manually register types in your project startup routine.

### Interfaces

Under some circumstances you do not want to register an AutoDI type under its own type,
but rather an interface it implements.
To achieve this, you can add the interface type to the attribute declaration:

```C#
[AutoRegister(RegistrationType.None, typeof(ISomething))]
class Something {/*...*/}
```

Note: AutoDI doesn't checks if the type actually implements the specified interface.
