using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AyrA.AutoDI
{
    /// <summary>
    /// Provides extension methods to set up automated dependency injection
    /// </summary>
    public static class AutoDIExtensions
    {
        /// <summary>
        /// Gets or sets if messages should be written to <see cref="Logger"/> and attached debug listeners
        /// </summary>
        /// <remarks>
        /// Unless necessary, this should be left disabled because it can slow down loading significantly.
        /// </remarks>
        public static bool DebugLogging { get; set; }

        /// <summary>
        /// Gets or sets the logger that outputs debug messages
        /// </summary>
        public static TextWriter Logger { get; set; } = Console.Error;

        /// <summary>
        /// Gets a list of excluded names.
        /// Any assembly (not type) starting with a prefix from this list will be skipped
        /// </summary>
        /// <remarks>
        /// This is case sensitive and only has an effect on <see cref="AutoRegisterAllAssemblies"/>.
        /// The default is to exclude libraries starting with "System." and "Microsoft." as well as "AyrA.AutoDI" itself
        /// </remarks>
        public static List<string> NameExclusions { get; } = new() { "AyrA.AutoDI", "Microsoft.", "System." };

        /// <summary>
        /// Automatically registers all AutoDI types from all loaded assemblies
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="throwOnNoneType">Throw an exception if attempting to register a type with <see cref="AutoDIType.None"/></param>
        /// <returns><paramref name="collection"/></returns>
        /// <exception cref="InvalidOperationException">
        /// A type has <see cref="AutoDIType.None"/> set, and <paramref name="throwOnNoneType"/> was enabled
        /// </exception>
        /// <remarks>
        /// For big projects, this can take a very long time
        /// </remarks>
        public static IServiceCollection AutoRegisterAllAssemblies(this IServiceCollection collection, bool throwOnNoneType = false)
        {
            Log("Loading all AutoDI types from all referenced assemblies");

            //Any assembly in this list will neither be processed nor loaded
            List<string> loaded = new();

            //Assemblies in this list will be processed unless they're in the loaded name list
            Stack<Assembly> assemblies = new(AppDomain.CurrentDomain.GetAssemblies());
            while (assemblies.Count > 0)
            {
                Assembly asm = assemblies.Pop();
                var asmName = asm.GetName();
                if(asmName?.Name == null)
                {
                    Log($"Unnamed: {asm}");
                    continue;
                }
                if (IsExcludedAssembly(asmName))
                {
                    Log($"Excluded: {asmName}");
                    continue;
                }
                if (!loaded.Contains(asmName.Name))
                {
                    loaded.Add(asmName.FullName);
                    collection.AutoRegisterFromAssembly(asm, throwOnNoneType);
                    //Recursively process referenced assemblies
                    foreach (var name in asm.GetReferencedAssemblies())
                    {
                        //Don't try to load assemblies we've already processed or are excluded
                        if (!IsExcludedAssembly(name))
                        {
                            if (!loaded.Contains(name.FullName))
                            {
                                assemblies.Push(AppDomain.CurrentDomain.Load(name));
                            }
                        }
                        else
                        {
                            Log($"Excluded: {name}");
                        }
                    }
                }
            }
            return collection;
        }

        /// <summary>
        /// Scans the calling assembly for all types and injects those marked with
        /// <see cref="AutoDIRegisterAttribute"/>
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="throwOnNoneType">Throw an exception if attempting to register a type with <see cref="AutoDIType.None"/></param>
        /// <returns><paramref name="collection"/></returns>
        /// <exception cref="InvalidOperationException">
        /// A type has <see cref="AutoDIType.None"/> set, and <paramref name="throwOnNoneType"/> was enabled
        /// </exception>
        public static IServiceCollection AutoRegisterCurrentAssembly(this IServiceCollection collection, bool throwOnNoneType = false)
        {
            return collection.AutoRegisterFromAssembly(Assembly.GetCallingAssembly(), throwOnNoneType);
        }

        /// <summary>
        /// Scans the specified assembly for all types and injects those marked with
        /// <see cref="AutoDIRegisterAttribute"/>
        /// </summary>
        /// <param name="assembly">Assembly to scan for types</param>
        /// <param name="collection">Service collection</param>
        /// <param name="throwOnNoneType">Throw an exception if attempting to register a type with <see cref="AutoDIType.None"/></param>
        /// <returns><paramref name="collection"/></returns>
        /// <exception cref="InvalidOperationException">
        /// A type has <see cref="AutoDIType.None"/> set, and <paramref name="throwOnNoneType"/> was enabled
        /// </exception>
        public static IServiceCollection AutoRegisterFromAssembly(this IServiceCollection collection, Assembly assembly, bool throwOnNoneType = false)
        {
            Log($"Scanning {assembly.FullName} for AutoDI types");
            foreach (var t in assembly.GetTypes())
            {
                if (t.IsClass)
                {
                    if (t.GetConstructors().Length == 0)
                    {
                        Log($"Skipping {t}: Has no constructors");
                    }
                    else if (t.GetCustomAttributes<AutoDIRegisterAttribute>().Any())
                    {
                        collection.AutoRegister(t, throwOnNoneType);
                    }
                    else
                    {
                        Log($"Skipping {t}: Has no {nameof(AutoDIRegisterAttribute)}");
                    }
                }
                else
                {
                    Log($"Skipping {t}: Not a class");
                }
            }
            return collection;
        }

        /// <summary>
        /// Registers the specified type for dependency injection
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="type">Type to register</param>
        /// <param name="throwOnNoneType">Throw an exception if attempting to register a type with <see cref="AutoDIType.None"/></param>
        /// <returns><paramref name="collection"/></returns>
        /// <exception cref="ArgumentException">
        /// Undefined enum value in <paramref name="collection"/> or
        /// <paramref name="type"/> doen't bears the <see cref="AutoDIRegisterAttribute"/> attribute
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// A type has <see cref="AutoDIType.None"/> set, and <paramref name="throwOnNoneType"/> was enabled
        /// </exception>
        public static IServiceCollection AutoRegister(this IServiceCollection collection, Type type, bool throwOnNoneType = false)
        {
            var attrList = type.GetCustomAttributes<AutoDIRegisterAttribute>().ToList();
            if (attrList == null || attrList.Count == 0)
            {
                throw new ArgumentException($"Type {type} doesn't bears {nameof(AutoDIRegisterAttribute)} attribute");
            }
            foreach (var attr in attrList)
            {
                Log($"registration type of {type} is {attr.RegistrationType}");
                switch (attr.RegistrationType)
                {
                    case AutoDIType.Singleton:
                        Register(ServiceCollectionServiceExtensions.AddSingleton, collection, attr.InterfaceType, type);
                        break;
                    case AutoDIType.Transient:
                        Register(ServiceCollectionServiceExtensions.AddTransient, collection, attr.InterfaceType, type);
                        break;
                    case AutoDIType.Scoped:
                        Register(ServiceCollectionServiceExtensions.AddScoped, collection, attr.InterfaceType, type);
                        break;
                    case AutoDIType.None:
                        if (throwOnNoneType)
                        {
                            throw new InvalidOperationException($"Type {type} has registration set to {nameof(AutoDIType.None)}, and {nameof(throwOnNoneType)} is enabled");
                        }
                        break;
                    default:
                        throw new ArgumentException($"{attr.RegistrationType} is not a valid registration type");
                }
            }
            return collection;
        }

        /// <summary>
        /// Call DI registration function
        /// </summary>
        /// <param name="registerFunction">registration function</param>
        /// <param name="collection">Service collection</param>
        /// <param name="interfaceType">Interface type. If null, uses <paramref name="implementationType"/></param>
        /// <param name="implementationType">Type that implements <paramref name="interfaceType"/></param>
        /// <returns><paramref name="collection"/></returns>
        private static IServiceCollection Register(Func<IServiceCollection, Type, Type, IServiceCollection> registerFunction, IServiceCollection collection, Type? interfaceType, Type implementationType)
        {
            Log($"Registering {implementationType} in the service collection as {interfaceType ?? implementationType}...");
            var ret = registerFunction(collection, interfaceType ?? implementationType, implementationType);
            Log($"Done");
            return ret;
        }

        /// <summary>
        /// Writes a log line
        /// </summary>
        /// <param name="message">Log message</param>
        private static void Log(string message)
        {
            if (DebugLogging)
            {
                Logger.WriteLine($"AutoDI: {message}");
                Debug.Print($"AutoDI: {message}");
            }
        }

        /// <summary>
        /// Gets if the assembly name is excluded from scanning
        /// </summary>
        /// <param name="name">Assembly name</param>
        /// <returns>true, if part of the exclusion list</returns>
        private static bool IsExcludedAssembly(AssemblyName name)
        {
            if (NameExclusions.Count == 0 || name.Name == null)
            {
                return false;
            }
            return NameExclusions.Any(m => name.Name.StartsWith(m));
        }
    }
}