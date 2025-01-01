using SimpleEnterpriseFramework.Data;

namespace SimpleEnterpriseFramework.App;

public abstract class Application(IDatabaseDriver db)
{
    protected IDatabaseDriver Db { get; } = db;
    private readonly HashSet<string> _registeredForms = [];

    public void RegisterForms<TModel>(List<Form<TModel>> forms)
        where TModel : Model, new()
    {
        foreach (var form in forms)
        {
            if (!_registeredForms.Add(form.TableName))
            {
                throw new Exception($"Form with table name {form.TableName} is already registered.");
            }

            RegisterForm(form);
        }
    }

    protected abstract void RegisterForm<TModel>(Form<TModel> form) where TModel : Model, new();
}