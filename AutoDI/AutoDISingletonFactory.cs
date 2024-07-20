using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;

namespace AyrA.AyrA.AutoDI
{
    internal static class AutoDISingletonFactory
    {
        private static readonly MethodInfo generic =
            typeof(AutoDISingletonFactory).GetMethod(nameof(Register), BindingFlags.Static | BindingFlags.NonPublic, [typeof(IServiceCollection)])
            ?? throw new InvalidOperationException("Register function not found");

        internal static void Register<T>(IServiceCollection collection) where T : class, IHostedService
        {
            collection.AddSingleton<T>();
            collection.AddHostedService((sp) => sp.GetRequiredService<T>());
        }

        internal static void Register(IServiceCollection collection, Type hostType)
        {
            generic.MakeGenericMethod(hostType).Invoke(null, [collection]);
        }
    }
}
