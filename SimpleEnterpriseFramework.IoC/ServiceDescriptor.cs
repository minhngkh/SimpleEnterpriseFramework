namespace SimpleEnterpriseFramework.IoC;

public class ServiceDescriptor
{
    public Type ServiceType { get; }
    public Type ImplementationType { get; }
    public ServiceLifetime Lifetime { get; }

    public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
    }
    
    public ServiceDescriptor(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        
        
        if (Attribute.GetCustomAttribute(implementationType, typeof(InjectionAttribute)) is InjectionAttribute attribute)
        {
            Lifetime = attribute.Lifetime;
        }
        else
        {
            throw new InvalidOperationException(
                $"Service {implementationType} does not have InjectionAttribute.");
        }
    }
}