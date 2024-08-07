﻿using AyrA.AyrA.AutoDI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AyrA.AutoDI
{
    /// <summary>
    /// Provides extension methods to set up automated dependency injection
    /// </summary>
    public static class AutoDIExtensions
    {
        /// <summary>
        /// Name of the method that is used for the registration of hosted services
        /// </summary>
        private const string hostingMethodName = nameof(ServiceCollectionHostedServiceExtensions.AddHostedService);
        /// <summary>
        /// Public static methods
        /// </summary>
        private const BindingFlags FlagsPublic = BindingFlags.Static | BindingFlags.Public;
        /// <summary>
        /// Private static methods
        /// </summary>
        private const BindingFlags FlagsPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly Type[] RegisterArgs1 =
        [
            typeof(IServiceCollection)
        ];
        private static readonly Type[] RegisterArgs2 =
        [
            typeof(IServiceCollection), typeof(AutoDIRegisterAttribute)
        ];
        private static readonly MethodInfo hostingRegistrationMethod;

        private static string[] currentFilters = [];

        /// <summary>
        /// Gets or sets if messages should be written to <see cref="Logger"/> and attached debug listeners
        /// </summary>
        /// <remarks>
        /// Unless necessary, this should be left disabled
        /// because it can slow down loading significantly.
        /// </remarks>
        public static bool DebugLogging { get; set; }

        /// <summary>
        /// Gets or sets the logger that outputs debug messages
        /// </summary>
        /// <remarks>
        /// This has no effect if <see cref="DebugLogging"/> is not enabled.
        /// Defaults to <see cref="Console.Error"/>
        /// </remarks>
        public static TextWriter Logger { get; set; } = Console.Error;

        /// <summary>
        /// Gets a list of excluded names.
        /// Any assembly (not type) starting with a prefix from this list will be skipped
        /// </summary>
        /// <remarks>
        /// This is case sensitive and only has an effect on <see cref="AutoRegisterAllAssemblies"/>.
        /// The default is to exclude libraries starting with "System." and "Microsoft." as well as "AyrA.AutoDI" itself
        /// </remarks>
        public static List<string> NameExclusions { get; } =
        [
            "AyrA.AutoDI",
            "Microsoft.",
            "System."
        ];

        /// <summary>
        /// Gets a list of filters.
        /// AutoDI will only add types which have an empty filter list,
        /// or where at least one item of the two lists overlap
        /// </summary>
        /// <remarks>If this is empty, all attributes with any filter will be disabled</remarks>
        public static List<string> FilterList { get; } = [];

        static AutoDIExtensions()
        {
            hostingRegistrationMethod = typeof(ServiceCollectionHostedServiceExtensions).GetMethod(hostingMethodName, FlagsPublic, [typeof(IServiceCollection)])
                ?? throw new InvalidOperationException($"Unable to obtain generic service registration method. Does '{hostingMethodName}' no longer exist?");
        }

        /// <summary>
        /// Automatically registers all AutoDI types from all loaded assemblies
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <returns><paramref name="collection"/></returns>
        /// <remarks>
        /// For big projects, this can take a very long time
        /// </remarks>
        public static IServiceCollection AutoRegisterAllAssemblies(this IServiceCollection collection)
        {
            Log("Loading all AutoDI types from all referenced assemblies");
            PrepareFilters();

            //Any assembly in this list have already been processed
            //and will neither be processed nor loaded
            List<string> loaded = [];

            //Assemblies in this list will be processed unless they're in the loaded name list
            Stack<Assembly> assemblies = new(AppDomain.CurrentDomain.GetAssemblies());
            while (assemblies.Count > 0)
            {
                Assembly asm = assemblies.Pop();
                var asmName = asm.GetName();
                if (asmName?.Name == null)
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
                    collection.AutoRegisterFromAssembly(asm);
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
        /// <returns><paramref name="collection"/></returns>
        public static IServiceCollection AutoRegisterCurrentAssembly(this IServiceCollection collection)
        {
            return collection.AutoRegisterFromAssembly(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Scans the specified assembly for all types and injects those marked with
        /// <see cref="AutoDIRegisterAttribute"/>
        /// </summary>
        /// <param name="assembly">Assembly to scan for types</param>
        /// <param name="collection">Service collection</param>
        /// <returns><paramref name="collection"/></returns>
        public static IServiceCollection AutoRegisterFromAssembly(this IServiceCollection collection, Assembly assembly)
        {
            Log($"Scanning {assembly.FullName} for AutoDI types");
            PrepareFilters();
            foreach (var t in assembly.GetTypes())
            {
                if (t.IsClass)
                {
                    if (t.GetCustomAttributes<AutoDIRegisterAttribute>().Any() ||
                        t.GetCustomAttributes<AutoDIHostedServiceAttribute>().Any() ||
                        t.GetCustomAttributes<AutoDIHostedSingletonServiceAttribute>().Any())
                    {
                        collection.AutoRegister(t);
                    }
                    else
                    {
                        Log($"Skipping {t}: Has neither {nameof(AutoDIRegisterAttribute)} nor {nameof(AutoDIHostedServiceAttribute)}");
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
        /// <returns><paramref name="collection"/></returns>
        /// <remarks>Consider using one of the methods that registers entire assemblies instead</remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="type"/> has an invalid attribute combination
        /// </exception>
        private static IServiceCollection AutoRegister(this IServiceCollection collection, Type type)
        {
            var serviceAttrList = type
                .GetCustomAttributes<AutoDIRegisterAttribute>()
                .Where(Filter)
                .ToList();
            var hostAttrList = type
                .GetCustomAttributes<AutoDIHostedServiceAttribute>()
                .Where(Filter)
                .ToList();
            var combinedAttrList = type
                .GetCustomAttributes<AutoDIHostedSingletonServiceAttribute>()
                .Where(Filter)
                .ToList();
            if (serviceAttrList.Count + hostAttrList.Count + combinedAttrList.Count == 0)
            {
                Log($"{type.FullName} is skipped due to the current filter settings");
                return collection;
            }
            if (hostAttrList.Count > 1)
            {
                throw new ArgumentException($"Type {type.FullName} has multiple {nameof(AutoDIHostedServiceAttribute)} attributes. This is not allowed");
            }
            if (combinedAttrList.Count > 1)
            {
                throw new ArgumentException($"Type {type.FullName} has multiple {nameof(AutoDIHostedSingletonServiceAttribute)} attributes. This is not allowed");
            }
            if (combinedAttrList.Count > 0 && hostAttrList.Count > 0)
            {
                throw new ArgumentException($"Type {type.FullName} has both {nameof(AutoDIHostedServiceAttribute)} and {nameof(AutoDIHostedSingletonServiceAttribute)} attributes. This is not allowed");
            }
            //Service registration
            foreach (var attr in serviceAttrList)
            {
                Log($"registration type of {type.FullName} is {attr.RegistrationType}");
                switch (attr.RegistrationType)
                {
                    case AutoDIType.Singleton:
                    case AutoDIType.Transient:
                    case AutoDIType.Scoped:
                        Register(collection, attr, type);
                        break;
                    case AutoDIType.Custom:
                        RegisterCustom(collection, attr, type);
                        break;
                    default:
                        throw new ArgumentException($"{attr.RegistrationType} is not a valid registration type");
                }
            }
            //Hosting registration
            if (hostAttrList.Count > 0)
            {
                RegisterHost(collection, type);
            }
            //Combined registration
            if (combinedAttrList.Count > 0)
            {
                RegisterCombined(collection, type);
            }
            return collection;
        }

        /// <summary>
        /// Call DI registration function
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="attr">AutoDI Attribute for the current registration instance</param>
        /// <param name="implementationType">Implementation type to be registered</param>
        /// <returns><paramref name="collection"/></returns>
        private static IServiceCollection Register(IServiceCollection collection, AutoDIRegisterAttribute attr, Type implementationType)
        {
            if (attr.RegisterFunction != null)
            {
                throw new TypeRegistrationException($"Automatic registration not possible. Type {implementationType.FullName} has custom registration function set");
            }
            else
            {
                IServiceCollection result;
                Log($"Registering {implementationType.FullName} in the service collection as {attr.InterfaceType ?? implementationType} using default registration implementation...");
                switch (attr.RegistrationType)
                {
                    case AutoDIType.Singleton:
                        result = ServiceCollectionServiceExtensions.AddSingleton(collection, attr.InterfaceType ?? implementationType, implementationType);
                        break;
                    case AutoDIType.Transient:
                        result = ServiceCollectionServiceExtensions.AddTransient(collection, attr.InterfaceType ?? implementationType, implementationType);
                        break;
                    case AutoDIType.Scoped:
                        result = ServiceCollectionServiceExtensions.AddScoped(collection, attr.InterfaceType ?? implementationType, implementationType);
                        break;
                    default:
                        Log($"Invalid registration type {attr.RegistrationType} for {implementationType.FullName}");
                        throw new TypeRegistrationException($"Invalid registration type {attr.RegistrationType} for {implementationType.FullName}");
                }
                Log("Done");
                return result;
            }
        }

        /// <summary>
        /// Preform custom registration of a type
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="attr">AutoDI attribute</param>
        /// <param name="implementationType">Implementation type</param>
        /// <returns><paramref name="collection"/></returns>
        private static IServiceCollection RegisterCustom(IServiceCollection collection, AutoDIRegisterAttribute attr, Type implementationType)
        {
            if (attr.RegisterFunction == null)
            {
                throw new TypeRegistrationException($"Type {implementationType.FullName} has no custom registration function in the AutoDI attribute");
            }
            Log($"Registering {implementationType} in the service collection as {attr.InterfaceType ?? implementationType} using custom registration implementation in '{attr.RegisterFunction}'...");
            var func =
                //Try the two argument version first
                (implementationType.GetMethod(attr.RegisterFunction, FlagsPublic, RegisterArgs2) ??
                implementationType.GetMethod(attr.RegisterFunction, FlagsPrivate, RegisterArgs2) ??
                //Try with only a single argument last
                implementationType.GetMethod(attr.RegisterFunction, FlagsPublic, RegisterArgs1) ??
                implementationType.GetMethod(attr.RegisterFunction, FlagsPrivate, RegisterArgs1)) ??
                throw new TypeRegistrationException($"The type '{implementationType.FullName}' has no matching static registration method named '{attr.RegisterFunction}', but a custom function was specified in the attribute");
            object[] funcParams = func.GetParameters().Length == 2 ? [collection, attr] : [collection];
            try
            {
                func.Invoke(null, funcParams);
            }
            catch (Exception ex)
            {
                throw new TypeRegistrationException($"Custom registration function '{attr.RegisterFunction}' in type '{implementationType.FullName}' threw an exception when called. See inner exception for details.", ex);
            }
            Log("Done");
            return collection;
        }

        /// <summary>
        /// Register a hosted service
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="hostType">Host service type</param>
        /// <returns><paramref name="collection"/></returns>
        private static IServiceCollection RegisterHost(IServiceCollection collection, Type hostType)
        {
            if (!hostType.IsAssignableTo(typeof(IHostedService)))
            {
                throw new TypeRegistrationException($"The type '{hostType.FullName}' does not implement '{typeof(IHostedService).FullName}'. This interface must be implemented to use {nameof(AutoDIHostedServiceAttribute)}");
            }
            hostingRegistrationMethod.MakeGenericMethod(hostType).Invoke(null, [collection]);
            return collection;
        }

        /// <summary>
        /// Register a combined singleton and hosted service
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="hostType">Host service type</param>
        /// <returns><paramref name="collection"/></returns>
        private static IServiceCollection RegisterCombined(IServiceCollection collection, Type hostType)
        {
            if (!hostType.IsAssignableTo(typeof(IHostedService)))
            {
                throw new TypeRegistrationException($"The type '{hostType.FullName}' does not implement '{typeof(IHostedService).FullName}'. This interface must be implemented to use {nameof(AutoDIHostedSingletonServiceAttribute)}");
            }
            AutoDISingletonFactory.Register(collection, hostType);
            return collection;
        }

        /// <summary>
        /// Writes a log line
        /// </summary>
        /// <param name="message">Log message</param>
        private static void Log(string message)
        {
            if (DebugLogging)
            {
                Logger.WriteLine("AutoDI: {0}", message);
                Debug.Print("AutoDI: {0}", message);
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
            return NameExclusions.Any(name.Name.StartsWith);
        }

        private static void PrepareFilters()
        {
            currentFilters = [.. FilterList.Select(m => m.Trim().ToLowerInvariant()).Distinct()];
        }

        private static bool Filter(AutoDIFilterAttribute attr)
        {
            return attr.IsFilterMatch([.. FilterList]);
        }
    }
}