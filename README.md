# Egonsoft.HU DependencyInjection Extensions Abstractions

[![GitHub](https://img.shields.io/github/license/gcsizmadia/EgonsoftHU.Extensions.DependencyInjection.Abstractions?label=License)](https://opensource.org/licenses/MIT)
[![Nuget](https://img.shields.io/nuget/v/EgonsoftHU.Extensions.DependencyInjection.Abstractions?label=NuGet)](https://www.nuget.org/packages/EgonsoftHU.Extensions.DependencyInjection.Abstractions)
[![Nuget](https://img.shields.io/nuget/dt/EgonsoftHU.Extensions.DependencyInjection.Abstractions?label=Downloads)](https://www.nuget.org/packages/EgonsoftHU.Extensions.DependencyInjection.Abstractions)

Abstractions for dependency injection.

Commonly Used Types:
- `EgonsoftHU.Extensions.DependencyInjection.IAssemblyRegistry`
- `EgonsoftHU.Extensions.DependencyInjection.AssemblyRegistry`

## Table of Contents

- [Introduction](#introduction)
- [Releases](#releases)
- [Instructions](#instructions)
- [Example](#example)

## Introduction

The motivation behind this project is to automatically discover and load all _relevant_ assemblies into the current `AppDomain` and make
these assemblies available when needed, e.g. for registering dependencies using assembly scanning (e.g. FluentValidation).

## Releases

You can download the package from **nuget.org**: [EgonsoftHU.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/EgonsoftHU.Extensions.DependencyInjection.Abstractions)

You can find the release notes [here](https://github.com/gcsizmadia/dependencyinjection/releases).

[Releases](RELEASES.md) under active support or development:

|Version|Release Date|Latest Patch Version|End of Support|Targets|
|-|-|-|-|-|
|11.0|2026-11-24|11.0.0|2028-11-14|`net10.0`, `net11.0`<br/>`net472`<br/>`netstandard2.0`, `netstandard2.1`|
|10.0|2026-11-24|10.0.0|2028-11-14|`net8.0`, `net9.0`, `net10.0`<br/>`net472`<br/>`netstandard2.0`, `netstandard2.1`|

## Instructions

Since version 10.0, the `DefaultAssemblyRegistry` type is no longer available directly.

Instead, use one of the methods of `AssemblyRegistry` type to configure an assembly registry implementation.  
You may use either the built-in default implementation or your custom implementation.

The configured assembly registry is available through the `AssemblyRegistry.Current` property.

### Empty implementation

If none of the overloads of either the `AssemblyRegistry.UseDefault()` or the `AssemblyRegistry.UseCustom()` methods is called
then an empty implementation will be used.

This implementation always returns an empty array of `Assembly` type.

### Default implementation

To use the default implementation, call one of the overloads of the `AssemblyRegistry.UseDefault()` method.

The default implementation can be configured:
- To use assembly file name prefixes.
  - As you did with the `DefaultAssemblyRegistry` type in the previous versions.
- To use custom assembly file path predicate.
  - You may specify a `Func<string, bool>` delegate that receives the full path of an assembly file.
- To use an instance of `Microsoft.Extensions.Logging.ILoggerFactory` for logging.
  - By default, the `Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance` is used.

**Please note:** `EgonsoftHU.*.dll` search pattern is always added to the search.

```csharp
using System.IO;
using EgonsoftHU.Extensions.DependencyInjection;

// This will search for assemblies as below then loads them into the current AppDomain:
//     Root Folder : AppContext.BaseDirectory
//     Pattern #0  : EgonsoftHU.*.dll
//     Pattern #1  : MyCompany.*.dll
//     Pattern #2  : Custom.*.dll
//     SearchOption: SearchOption.AllDirectories
AssemblyRegistry.UseDefault("MyCompany", "Custom");

// To add an ILoggerFactory instance
AssemblyRegistry.UseDefault(loggerFactory, "MyCompany", "Custom");

// To use a predicate for assembly file paths
// You can also configure the assembly file name prefixes and the ILoggerFactory here.
AssemblyRegistry.UseDefault(
    options =>
    {
        options.AssemblyFileNamePrefixes = [ "MyCompany", "Custom" ];
        options.AssemblyFilePredicate = filePath => Path.GetFileName(filePath) == "magic.dll";
        options.LoggerFactory = loggerFactory;
    }
);
```

### Custom implementation

The expectation for a custom implementation is to have a method that meets the following criteria:
- the method must be public
- the method must be non-static
- the method must be parameterless
- the method name must be `GetAssemblies`
- the return type must be either `IEnumerable<Assembly>` or a compatible type

As an option, you may use the `EgonsoftHU.Extensions.DependencyInjection.IAssemblyRegistry` interface to conveniently implement your custom assembly registry.

```csharp
using EgonsoftHU.Extensions.DependencyInjection;

// If you implement the interface and your implementation has a parameterless constructor.
// UseCustom<T>() where T : IAssemblyRegistry, new()
AssemblyRegistry.UseCustom<YourCustomAssemblyRegistry>();

// If you implement the interface but your implementation requires constructor parameter(s).
// UseCustom(IAssemblyRegistry)
AssemblyRegistry.UseCustom(new YourCustomAssemblyRegistry("some parameter"));

// If you do not implement the interface.
// UseCustom(object)
AssemblyRegistry.UseCustom(new YourCustomAssemblyRegistry());
```

As for the last overload, your assembly registry instance will be wrapped into an instance of the `ObjectWrapperAssemblyRegistry` type
that implements the `IAssemblyRegistry` interface.

This wrapper detects your custom method in your implementation using reflection and stores the `MethodInfo` instance so that calling
the `GetAssemblies()` method on the wrapper type will simply invoke your method on your custom assembly registry instance and returns
its result.

If the wrapper type does not find a suitable method in your implementation then an `ArgumentException` will be thrown from the `UseCustom()` method.

### Getting the relevant assemblies

Whenever you need the relevant assemblies, just call the `GetAssemblies()` method on the `AssemblyRegistry.Current` property.

```csharp
using EgonsoftHU.Extensions.DependencyInjection;

var assemblies = AssemblyRegistry.Current.GetAssemblies();
```

### Example

Suppose you have an ASP.NET Core project that needs to load controllers from other projects.

Let's create an extension method that will do the magic.

```csharp
using System.Reflection;

using EgonsoftHU.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddApplicationParts(this IMvcBuilder builder)
    {
        // This is the assembly of your ASP.NET Core startup project.
        // We will not call the AddApplicationPart() method for this assembly.
        Assembly entryAssembly = Assembly.GetEntryAssembly();

        AssemblyRegistry
            .Current
            .GetAssemblies()
            .Where(
                assembly =>
                assembly != entryAssembly
                &&
                assembly.DefinedTypes.Any(typeInfo => typeof(ControllerBase).IsAssignableFrom(typeInfo))
            )
            .ToList()
            .ForEach(assembly => builder.AddApplicationPart(assembly));

        return builder;
    }
}
```

Now you can use it in your `Program.cs` file.

```csharp
using EgonsoftHU.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

// Initialize the assembly registry.
AssemblyRegistry.UseDefault("YourCompany", "Custom");

services
    .AddMvc()
    .AddApplicationParts(); // <-- This will load controllers, view components, or tag helpers from all assemblies other than the entry assembly.

// You can also use the assembly registry when registering dependencies using assembly scanning. (e.g. FluentValidation)
services.AddValidatorsFromAssemblies(AssemblyRegistry.Current.GetAssemblies());
```
