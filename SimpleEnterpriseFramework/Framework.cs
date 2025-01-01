using SimpleEnterpriseFramework.App;
using SimpleEnterpriseFramework.Data;
using SimpleEnterpriseFramework.IoC;

namespace SimpleEnterpriseFramework;

public class Framework
{
    private readonly Container _container = new();

    private Membership? _membership;
    public Membership Membership => _membership ??= _container.Resolve<Membership>();

    public void SetDatabaseDriver<TDriver, TOptions>(Action<TOptions> options) where TDriver : IDatabaseDriver
    {
        _container.RegisterSingleton<IDatabaseDriver, TDriver>();
        _container.Configure(options);
        
    }

    public T CreateCrudApp<T>() where T : CrudApp
    {
        return _container.Resolve<T>();
    }
}