using System;

namespace AyrA.AutoDI
{
    /// <summary>
    /// Registers a type for automatic dependency injection
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoDIRegisterAttribute : Attribute
    {
        /// <summary>
        /// Registers a type for automatic dependency injection
        /// </summary>
        /// <param name="registrationType">DI type</param>
        /// <param name="interfaceType">
        /// Implemented interface.
        /// If null, type is registered as itself</param>
        /// <exception cref="ArgumentException">Undefined enum value in <paramref name="registrationType"/></exception>
        public AutoDIRegisterAttribute(AutoDIType registrationType, Type? interfaceType = null)
        {
            if (!Enum.IsDefined(registrationType))
            {
                throw new ArgumentException($"Enum not defined: {registrationType}");
            }
            RegistrationType = registrationType;
            InterfaceType = interfaceType;
        }

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