using System.Diagnostics.CodeAnalysis;

namespace SimpleEnterpriseFramework.IoC;

public class SingletonStore
{
    private readonly Dictionary<Type, object> _singletonDict = new();

    public void Add<TService>(TService instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        _singletonDict[typeof(TService)] = instance;
    }
    
    public void Add(Type serviceType, object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        _singletonDict[serviceType] = instance;
    }
    
    public TService? TryGet<TService>()
    {
        return _singletonDict.TryGetValue(typeof(TService), out var instance)
            ? (TService)instance
            : default;
    }
    
    public bool TryGet(Type serviceType, [MaybeNullWhen(false)] out object instance)
    {
        return _singletonDict.TryGetValue(serviceType, out instance);
    }
}