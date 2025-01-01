using SimpleEnterpriseFramework.Abstractions;
using SimpleEnterpriseFramework.Abstractions.App;
using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.IoC;
using SimpleEnterpriseFramework.Membership;

namespace SimpleEnterpriseFramework.Core;

public class Framework
{
    private readonly Container _container = new();

    private IMembership? _membership;
    public IMembership Membership => _membership ??= _container.Resolve<IMembership>();

    public Framework(Action<MembershipOptions> options)
    {
        _container.RegisterSingleton<IMembership, Membership.Membership>();
        _container.Configure(options);
    }

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