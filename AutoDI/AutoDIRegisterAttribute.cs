using Microsoft.Extensions.DependencyInjection;
using System;

namespace AyrA.AutoDI
{
    /// <summary>
    /// Performs custom DI registration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="registrationAttribute">Attribute instance for the current type</param>
    public delegate void CustomRegistrationFunction(IServiceCollection services, AutoDIRegisterAttribute registrationAttribute);

    /// <summary>
    /// Performs custom DI registration
    /// </summary>
    /// <param name="services">Service collection</param>
    public delegate void SimpleCustomRegistrationFunction(IServiceCollection services);

    /// <summary>
    /// Registers a type for automatic dependency injection
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoDIRegisterAttribute : Attribute
    {
        /// <summary>
        /// Registers a type for automatic dependency injection
        /// </summary>
        /// <param name="registrationType">DI type</param>
        /// <param name="interfaceType">
        /// Implemented interface.
        /// If null, type is registered as itself
        /// </param>
        /// <param name="customRegistrationFunction">
        /// An optional function name to be called for type registration.
        /// The function must be static and defined in the same type, but may be private.
        /// If not supplied, default registration logic is used
        /// </param>
        /// <exception cref="ArgumentException">Undefined enum value in <paramref name="registrationType"/></exception>
        public AutoDIRegisterAttribute(AutoDIType registrationType, Type? interfaceType = null, string? customRegistrationFunction = null)
        {
            if (!Enum.IsDefined(registrationType))
            {
                throw new ArgumentException($"Enum not defined: {registrationType}");
            }
            RegistrationType = registrationType;
            InterfaceType = interfaceType;
            RegisterFunction = customRegistrationFunction;
        }

        /// <summary>
        /// Gets the custom registration function.
        /// </summary>
        public string? RegisterFunction { get; }

        /// <summary>
        /// Gets the DI injection type this type is registered for
        /// </summary>
        public AutoDIType RegistrationType { get; }
        /// <summary>
        /// Gets the interface type this type is registered for
        /// </summary>
        /// <remarks>
        /// If this is null, the type is registered using itself,
        /// which is usually what you want unless your type implements some interface you want to call it by.
        /// </remarks>
        public Type? InterfaceType { get; }
    }
}