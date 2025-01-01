using SimpleEnterpriseFramework.IoC;
using SimpleEnterpriseFramework.IoC.Attributes;

namespace SimpleEnterpriseFramework.Tests;

public class IocTests
{
    public interface ITestService;

    public class TestService : ITestService;

    public interface IServiceWithDependency
    {
        ITestService TestService { get; }
    }

    public class ServiceWithDependency(ITestService testService) : IServiceWithDependency
    {
        public ITestService TestService { get; set; } = testService;
    }
    
    [ClassInjection(ServiceLifetime.Singleton)]
    public class SingletonService : ITestService;
    
    [ClassInjection(ServiceLifetime.Transient)]
    public class TransientService : ITestService;

    
    private IContainer _container;

    [SetUp]
    public void Setup()
    {
        _container = new Container();
    }

    [Test]
    public void Resolve_singleton_services_returns_same_instance()
    {
        _container.RegisterSingleton<ITestService, TestService>();

        var instance1 = _container.Resolve<ITestService>();
        var instance2 = _container.Resolve<ITestService>();

        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void Resolve_transient_services_returns_different_instances()
    {
        _container.RegisterTransient<ITestService, TestService>();

        var instance1 = _container.Resolve<ITestService>();
        var instance2 = _container.Resolve<ITestService>();

        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void Resolve_singleton_services_with_singleton_dependencies()
    {
        _container.RegisterSingleton<ITestService, TestService>();
        _container.RegisterSingleton<IServiceWithDependency, ServiceWithDependency>();

        var instance = _container.Resolve<IServiceWithDependency>();

        Assert.That(instance, Is.Not.Null);
        Assert.That(instance.TestService, Is.Not.Null);
    }

    [Test]
    public void
        Resolve_singleton_services_with_singleton_dependencies_returns_same_instance()
    {
        _container.RegisterSingleton<ITestService, TestService>();
        _container.RegisterSingleton<IServiceWithDependency, ServiceWithDependency>();

        var instance1 = _container.Resolve<IServiceWithDependency>();
        var instance2 = _container.Resolve<IServiceWithDependency>();

        Assert.That(instance1, Is.SameAs(instance2));
        Assert.That(instance1.TestService, Is.SameAs(instance2.TestService));
    }

    [Test]
    public void
        Resolve_transient_services_with_transient_dependencies_returns_different_instances()
    {
        _container.RegisterTransient<ITestService, TestService>();
        _container.RegisterTransient<IServiceWithDependency, ServiceWithDependency>();

        var instance1 = _container.Resolve<IServiceWithDependency>();
        var instance2 = _container.Resolve<IServiceWithDependency>();

        Assert.That(instance1, Is.Not.SameAs(instance2));
        Assert.That(instance1.TestService, Is.Not.SameAs(instance2.TestService));
    }

    [Test]
    public void
        Resolve_transient_services_with_singleton_dependencies_returns_different_instances_with_the_same_dependency()
    {
        _container.RegisterSingleton<ITestService, TestService>();
        _container.RegisterTransient<IServiceWithDependency, ServiceWithDependency>();

        var instance1 = _container.Resolve<IServiceWithDependency>();
        var instance2 = _container.Resolve<IServiceWithDependency>();

        Assert.That(instance1, Is.Not.SameAs(instance2));
        Assert.That(instance1.TestService, Is.SameAs(instance2.TestService));
    }
    
    [Test]
    public void Resolve_services_with_singleton_attribute_returns_same_instance()
    {
        _container.RegisterSingleton<ITestService, SingletonService>();

        var instance1 = _container.Resolve<ITestService>();
        var instance2 = _container.Resolve<ITestService>();

        Assert.That(instance1, Is.SameAs(instance2));
    }
    
    [Test]
    public void Resolve_services_with_transient_attribute_returns_different_instances()
    {
        _container.Register<ITestService, TransientService>();

        var instance1 = _container.Resolve<ITestService>();
        var instance2 = _container.Resolve<ITestService>();

        Assert.That(instance1, Is.Not.SameAs(instance2));
    }
    
    
    [Test]
    public void Register_service_with_no_injection_attribute_is_invalid()
    {
        TestDelegate register = () => _container.Register<ITestService, TestService>();
        Assert.Throws<InvalidOperationException>(register);
    }
}