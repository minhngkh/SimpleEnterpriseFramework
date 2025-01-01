using SimpleEnterpriseFramework.Data;
using SimpleEnterpriseFramework.IoC;

namespace SimpleEnterpriseFramework;

public class Framework
{
    private readonly Container _container;

    private Membership? _membership;
    public Membership Membership => _membership ??= _container.Resolve<Membership>();

    public Framework(IDatabaseDriver db)
    {
        _container = new Container();
        _container.RegisterSingleton(db);
    }

    public T CreateEditorApp<T>() where T : App.Application
    {
        return _container.Resolve<T>();
    }
}