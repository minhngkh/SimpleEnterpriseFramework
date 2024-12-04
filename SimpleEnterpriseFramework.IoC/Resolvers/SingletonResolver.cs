namespace SimpleEnterpriseFramework.IoC.Resolvers;

public class SingletonResolver
{
    private static readonly Dictionary<Type, object> _singletons = new();
    
    public static void RegisterSingleton<TService>(TService instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        _singletons[typeof(TService)] = instance;
    }

    public static TService ResolveSingleton<TService>()
    {
        if (!_singletons.TryGetValue(typeof(TService), out var service))
        {
            throw new InvalidOperationException($"Service of type {typeof(TService).Name} not found");
        }

        return (TService)service;
    }
}