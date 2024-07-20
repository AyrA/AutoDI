using AyrA.AyrA.AutoDI;
using System;

namespace AyrA.AutoDI
{
    /// <summary>
    /// Automatically registers the specified type as a hosted service as well as a singleton
    /// </summary>
    /// <remarks>
    /// This is a combination of <see cref="AutoDIHostedServiceAttribute"/>
    /// and <see cref="AutoDIRegisterAttribute"/> using <see cref="AutoDIType.Singleton"/>.
    /// It's a shortcut to ensure that both attributes get the same instance
    /// without manually having to write factory methods each time
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AutoDIHostedSingletonServiceAttribute : AutoDIFilterAttribute
    {
    }
}