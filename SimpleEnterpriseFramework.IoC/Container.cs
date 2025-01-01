using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SimpleEnterpriseFramework.IoC.Attributes;

namespace SimpleEnterpriseFramework.IoC;

public interface IContainer
{
    void Register<TService, TImplementation>()
        where TImplementation : TService;

    void RegisterSingleton<TService, TImplementation>() where TImplementation : TService;
    void RegisterSingleton<TService>(TService instance);
    void RegisterTransient<TService, TImplementation>() where TImplementation : TService;

    // void Register<TService>(Func<TService> factory);
    // void Register<TService>(Func<IContainer, TService> factory);
    // void Register<TService>(Func<IContainer, TService> factory, ServiceLifetime lifetime);

    TService Resolve<TService>();
    object Resolve(Type serviceType);
    bool TryResolve<TService>([MaybeNullWhen(false)] out TService service);
    bool TryResolve(Type serviceType, [MaybeNullWhen(false)] out object service);
}

public class Container : IContainer
{
    private readonly Dictionary<Type, ServiceDescriptor> _services = new();
    private readonly SingletonStore _singletonStore = new();


    public void Register<TService, TImplementation>()
        where TImplementation : TService
    {
        _services[typeof(TService)] = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation)
        );
    }

    public void RegisterSingleton<TService, TImplementation>()
        where TImplementation : TService
    {
        _services[typeof(TService)] = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            ServiceLifetime.Singleton
        );
    }

    public void RegisterSingleton<TService>(TService instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        _services[typeof(TService)] = new ServiceDescriptor(
            typeof(TService),
            instance.GetType(),
            ServiceLifetime.Singleton
        );
    }

    public void RegisterTransient<TService, TImplementation>()
        where TImplementation : TService
    {
        _services[typeof(TService)] = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            ServiceLifetime.Transient
        );
    }

    public void RegisterInstance<TService>(TService instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        _services[typeof(TService)] = new ServiceDescriptor(
            typeof(TService),
            instance.GetType(),
            ServiceLifetime.Singleton
        );
    }

    public TService Resolve<TService>()
    {
        if (!TryResolve<TService>(out var service))
        {
            throw new InvalidOperationException(
                $"Service of type {typeof(TService)} not found.");
        }

        return service;
    }

    public object Resolve(Type serviceType)
    {
        if (!TryResolve(serviceType, out var service))
        {
            throw new InvalidOperationException(
                $"Service of type {serviceType} not found.");
        }

        return service;
    }

    public bool TryResolve<TService>([MaybeNullWhen(false)] out TService service)
    {
        if (TryResolve(typeof(TService), out var serviceObj))
        {
            service = (TService)serviceObj;
            return true;
        }

        service = default;
        return false;
    }

    public bool TryResolve(Type serviceType, [MaybeNullWhen(false)] out object service)
    {
        if (!_services.TryGetValue(serviceType, out var descriptor))
        {
            service = default;
            return false;
        }

        switch (descriptor.Lifetime)
        {
            case ServiceLifetime.Singleton:
                if (_singletonStore.TryGet(serviceType, out service))
                {
                    return true;
                }

                if (TryCreateInstance(descriptor.ImplementationType, out service))
                {
                    _singletonStore.Add(serviceType, service);
                    return true;
                }

                return false;

            case ServiceLifetime.Transient:
                return TryCreateInstance(descriptor.ImplementationType, out service);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool TryCreateInstance(Type implementationType,
        [MaybeNullWhen(false)] out object instance)
    {
        var constructor = implementationType.GetConstructors().Single();
        var parameters = constructor.GetParameters()
            .Select(param => Resolve(param.ParameterType))
            .ToArray();

        instance = Activator.CreateInstance(implementationType, parameters);
        if (instance is null)
        {
            return false;
        }

        foreach (var property in implementationType
                     .GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                    BindingFlags.Instance)
                     .Where(p =>
                         p.CanWrite &&
                         p.GetCustomAttributes<PropertyInjectionAttribute>().Any()
                     ))
        {
            var propertyValue = Resolve(property.PropertyType);
            property.SetValue(instance, propertyValue);
        }

        return true;
    }
}