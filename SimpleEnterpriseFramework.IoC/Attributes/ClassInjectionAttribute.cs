namespace SimpleEnterpriseFramework.IoC.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ClassInjectionAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    
    public ClassInjectionAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}