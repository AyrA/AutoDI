using AyrA.AyrA.AutoDI;
using System;

namespace AyrA.AutoDI
{
    /// <summary>
    /// Registers a type for automatic dependency injection
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoDIRegisterAttribute : AutoDIFilterAttribute
    {
        /// <summary>
        /// Registers a type for automatic dependency injection
        /// </summary>
        /// <param name="registrationType">DI type</param>
        /// <exception cref="ArgumentException">
        /// Undefined or forbidden enum value in <paramref name="registrationType"/>
        /// </exception>
        public AutoDIRegisterAttribute(AutoDIType registrationType)
        {
            if (!Enum.IsDefined(registrationType))
            {
                throw new ArgumentException($"Enum not defined: {registrationType}", nameof(registrationType));
            }
            if (registrationType == AutoDIType.Custom)
            {
                throw new ArgumentException($"Value '{AutoDIType.Custom}' cannot be specified for {nameof(registrationType)}", nameof(registrationType));
            }
            RegistrationType = registrationType;
        }

        /// <summary>
        /// Registers a type for automatic dependency injection
        /// </summary>
        /// <param name="registrationType">DI type</param>
        /// <param name="interfaceType">Implemented interface to register under</param>
        /// <exception cref="ArgumentException">
        /// Undefined or forbidden enum value in <paramref name="registrationType"/>
        /// </exception>
        public AutoDIRegisterAttribute(AutoDIType registrationType, Type? interfaceType)
        {
            if (!Enum.IsDefined(registrationType))
            {
                throw new ArgumentException($"Enum not defined: {registrationType}", nameof(registrationType));
            }
            if (registrationType == AutoDIType.Custom)
            {
                throw new ArgumentException($"Value '{AutoDIType.Custom}' cannot be specified for {nameof(registrationType)}", nameof(registrationType));
            }
            RegistrationType = registrationType;
            InterfaceType = interfaceType;
        }

        /// <summary>
        /// Registers a type for automatic dependency injection.
        /// This assumes there is a function named "RegisterDI" declared in the type
        /// </summary>
        public AutoDIRegisterAttribute() : this("RegisterDI")
        {
        }

        /// <summary>
        /// Registers a type for automatic dependency injection
        /// </summary>
        /// <param name="registrationFunction">
        /// A function name to be called for type registration.
        /// The function must be static and defined in the same type, but may be declared private.
        /// </param>
        public AutoDIRegisterAttribute(string registrationFunction)
        {
            if (string.IsNullOrEmpty(registrationFunction))
            {
                throw new ArgumentException($"'{nameof(registrationFunction)}' cannot be null or empty.", nameof(registrationFunction));
            }
            RegistrationType = AutoDIType.Custom;
            RegisterFunction = registrationFunction;
        }

        /// <summary>
        /// Gets the custom registration function.
        /// No custom registration is performed if this is null
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