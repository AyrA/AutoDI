# AutoDI

Performs automatic dependency injection registration.
This allows you to have new types you create or add as libraries registered fully automatically.

## Installation

Install from nuget, then set up your application for automatic DI.
Usually this means finding the code where services are registered
(traditionally in `Startup.cs` but in more modern Projects also `Program.cs`).

    builder.Services.AutoRegisterAll();

