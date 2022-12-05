using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AutoDI
{
    public static class AutoDIExtensions
    {
        public static IServiceCollection AutoRegisterAll(this IServiceCollection collection)
        {
            return collection.AutoRegisterAll(Assembly.GetCallingAssembly());
        }

        public static IServiceCollection AutoRegisterAll(this IServiceCollection collection, Assembly a)
        {
            foreach (var t in a.GetTypes())
            {
                if (t.IsClass)
                {
                    collection.AutoRegister(t);
                }
            }
            return collection;
        }

        public static IServiceCollection AutoRegister(this IServiceCollection collection, Type t)
        {
            var attr = t.GetCustomAttribute<AutoDIAttribute>();
            if (attr != null)
            {
                switch (attr.RegistrationType)
                {
                    case AutoDIType.Singleton:
                        Register(ServiceCollectionServiceExtensions.AddSingleton, collection, attr.InterfaceType, t);
                        Console.WriteLine($"{t} registered as singleton");
                        break;
                    case AutoDIType.Transient:
                        Register(ServiceCollectionServiceExtensions.AddTransient, collection, attr.InterfaceType, t);
                        Console.WriteLine($"{t} registered as transient service");
                        break;
                    case AutoDIType.Scoped:
                        Register(ServiceCollectionServiceExtensions.AddScoped, collection, attr.InterfaceType, t);
                        Console.WriteLine($"{t} registered as scoped service");
                        break;
                    case AutoDIType.None:
                        Console.WriteLine($"{t} set to not auto register.");
                        break;
                    default:
                        throw new ArgumentException($"{attr.RegistrationType} is not a valid registration type");
                }
            }
            else
            {
                Console.WriteLine($"Skipping type {t} because it's not marked as auto register");
            }
            return collection;
        }

        private static IServiceCollection Register(Func<IServiceCollection, Type, Type, IServiceCollection> registerFunction, IServiceCollection collection, Type interfaceType, Type implementationType)
        {
            return registerFunction(collection, interfaceType ?? implementationType, implementationType);
        }
    }
}