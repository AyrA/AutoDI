using AyrA.AyrA.AutoDI;
using Microsoft.Extensions.Hosting;
using System;

namespace AyrA.AutoDI
{
    /// <summary>
    /// Automatically registers the specified type as a hosted service
    /// </summary>
    /// <remarks>
    /// This attribute is only valid on types that implement
    /// <see cref="IHostedService"/>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AutoDIHostedServiceAttribute : AutoDIFilterAttribute
    {
    }
}