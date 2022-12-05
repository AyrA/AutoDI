using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System;

namespace AutoDI
{
    /// <summary>
    /// Provides extension methods to set up automated dependency injection
    /// </summary>
    public static class AutoDIExtensions
    {
        /// <summary>
        /// Scans the calling assembly for all types and injects those marked with
        /// <see cref="AutoDIAttribute"/>
        /// </summary>
        /// <param name="collection">Service collection</param>
        /// <param name="throwOnNoneType">Throw an exception if attempting to register a type with <see cref="AutoDIType.None"/></param>
        /// <returns><paramref name="collection"/></returns>
        /// <exception cref="InvalidOperationException">
        /// a type has <see cref="AutoDIType.None"/> set, and <paramref name="throwOnNoneType"/> was enabled
        /// </exception>
        public static IServiceCollection AutoRegisterAll(this IServiceCollection collection, bool throwOnNoneType = false)
        {
            return collection.AutoRegisterAll(Assembly.GetCallingAssembly(), throwOnNoneType);
        }

        /// <summary>
        /// Scans the specified assembly for all types and injects those marked with
        /// <see cref="AutoDIAttribute"/>
        /// </summary>
        /// <param name="assembly">Assembly to scan for types</param>
        /// <param name="collection">Service collection</param>
        /// <param name="throwOnNoneType">Throw an exception if attempting to register a type with <see cref="AutoDIType.None"/></param>
        /// <returns><paramref name="collection"/></returns>
        /// <exception cref="InvalidOperationException">
        /// a type has <see cref="AutoDIType.None"/> set, and <paramref name="throwOnNoneType"/> was enabled
        /// </exception>
        public static IServiceCollection AutoRegisterAll(this IServiceCollection collection, Assembly assembly, bool throwOnNoneType = false)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (t.IsClass)
                {
                    if (t.GetCustomAttribute<AutoDIAttribute>() != null)
                    {
                        collection.AutoRegister(t, throwOnNoneType);
                    }
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
        /// <paramref name="type"/> doen't bears the <see cref="AutoDIAttribute"/> attribute
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// a type has <see cref="AutoDIType.None"/> set, and <paramref name="throwOnNoneType"/> was enabled
        /// </exception>
        public static IServiceCollection AutoRegister(this IServiceCollection collection, Type type, bool throwOnNoneType = false)
        {
            var attr = type.GetCustomAttribute<AutoDIAttribute>();
            if (attr == null)
            {
                throw new ArgumentException($"Type {type} doesn't bears {nameof(AutoDIAttribute)} attribute");
            }
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
            return registerFunction(collection, interfaceType ?? implementationType, implementationType);
        }
    }
}