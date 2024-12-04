namespace SimpleEnterpriseFramework.IoC;

[AttributeUsage(AttributeTargets.Class)]
public class InjectionAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    
    public InjectionAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}