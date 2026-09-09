using System.Reflection;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ServiceProviderExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a service to the dependency injection container with the specified service lifetime.
        /// Provides a concise syntax for registering services with different lifetimes.
        /// </summary>
        public IServiceCollection AddService(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(implementationType);

            return lifetime switch
            {
                ServiceLifetime.Transient => services.AddTransient(serviceType, implementationType),
                ServiceLifetime.Scoped => services.AddScoped(serviceType, implementationType),
                ServiceLifetime.Singleton => services.AddSingleton(serviceType, implementationType),
                _ => throw new ArgumentException($"Invalid ServiceLifetime: {lifetime}", nameof(lifetime))
            };
        }

        /// <summary>
        /// Automatically registers all concrete implementations of a specified interface within the current domain
        /// as services in the dependency injection container. Uses reflection to discover and register services,
        /// promoting convention-based registration and reducing manual setup.
        /// </summary>
        public IServiceCollection AddServices(Type interfaceType, ServiceLifetime lifetime)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(interfaceType);

            List<Type?> types = [.. AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null)!;
                    }
                })
                .Where(t => t is not null && interfaceType.IsAssignableFrom(t)
                            && t.IsClass && !t.IsAbstract)];

            // Register for each interface implemented that matches the requested interfaceType
            foreach (Type? implType in types)
                foreach (Type? service in implType!.GetInterfaces().Where(interfaceType.IsAssignableFrom))
                    _ = services.AddService(service, implType, lifetime);

            return services;
        }

        /// <summary>
        /// Determines whether a service of the specified type has been added to the dependency injection container.
        /// </summary>
        public bool IsAdded(Type type)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(type);

            return services.Any(d => d.ServiceType == type);
        }

        /// <summary>
        /// Determines whether the concrete implementation type of the specified type has been added to the dependency injection container.
        /// </summary>
        public bool IsImplementationAdded(Type type)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(type);

            return services.Any(d => d.ImplementationType == type);
        }
    }

    extension<T>(IServiceCollection services)
    {
        /// <summary>
        /// Retrieves a singleton instance of the specified service type from the dependency injection container,
        /// throwing an exception if the service is not registered as a singleton.
        /// </summary>
        public T GetSingletonInstance()
        {
            ArgumentNullException.ThrowIfNull(services);

            T? instance = ServiceProviderExtensions.GetSingletonInstanceOrNull<T>(services);

            return instance is null
                ? throw new InvalidOperationException($"Could not find singleton service: {typeof(T).FullName}") : instance;
        }

        /// <summary>
        /// Retrieves a singleton instance of the specified service type from the dependency injection container,
        /// returning null if the service is not registered or is not a singleton.
        /// </summary>
        public T? GetSingletonInstanceOrNull()
        {
            ArgumentNullException.ThrowIfNull(services);

            ServiceDescriptor? descriptor = services
                .FirstOrDefault(d => d.ServiceType == typeof(T) && d.Lifetime == ServiceLifetime.Singleton);

            return descriptor?.ImplementationInstance is T instance ? instance : default;
        }

        /// <summary>
        /// Determines whether a service of the specified type has been added to the dependency injection container.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "<Pending>")]
        public bool IsAdded()
            => IsAdded(services, typeof(T));

        /// <summary>
        /// Determines whether the concrete implementation type of the specified service type has been added to the dependency injection container.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "<Pending>")]
        public bool IsImplementationAdded()
            => IsImplementationAdded(services, typeof(T));
    }

    extension<T>(IServiceProvider provider) where T : class
    {
        /// <summary>
        /// Resolves and creates an instance of the specified service type using dependency injection,
        /// allowing for injection of constructor parameters.
        /// </summary>
        public T ResolveWith(params object[] parameters)
        {
            ArgumentNullException.ThrowIfNull(provider);
            return ActivatorUtilities.CreateInstance<T>(provider, parameters);
        }
    }
}
