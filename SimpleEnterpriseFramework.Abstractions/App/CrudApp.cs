using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.IoC;

namespace SimpleEnterpriseFramework.Abstractions.App;

public abstract class CrudApp
{
    protected IDatabaseDriver Db { get; }
    private readonly IContainer _container;

    private readonly HashSet<Type> _registeredForms = [];

    protected CrudApp(IDatabaseDriver db)
    {
        Db = db;
        _container = new Container();
        _container.RegisterSingleton(db);
        
        // if (object.ReferenceEquals(db, _container.Resolve<IDatabaseDriver>()))
        // {
        //     Console.WriteLine("Database driver registered successfully.");
        // }
        // else
        // {
        //     Console.WriteLine("Database driver registration failed.");
        // }
    }
    
    
    
    public void RegisterForm<TModel, TForm>()
        where TModel : Model, new()
        where TForm : Form<TModel>
    {
        if (!_registeredForms.Add(typeof(TForm)))
        {
            throw new Exception(
                $"Form of type {typeof(TForm).Name} is already registered.");
        }

        var form = CreateForm<TModel, TForm>();

        RegisterForm(form);
    }

    protected abstract void RegisterForm<TModel>(Form<TModel> form)
        where TModel : Model, new();

    private TForm CreateForm<TModel, TForm>()
        where TModel : Model, new()
        where TForm : Form<TModel>
    {
        return _container.Resolve<TForm>();
    }
}