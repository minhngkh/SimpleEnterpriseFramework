using SimpleEnterpriseFramework.Data;

namespace SimpleEnterpriseFramework.App;

public abstract class Form<T> where T : Model, new()
{
    private IDatabaseDriver _db;
    public string TableName { get; } = typeof(T).Name;

    public Form(IDatabaseDriver db)
    {
        _db = db;
    }

    public virtual void Add(T obj)
    {
        _db.Add(obj);
    }

    public virtual void Add(Dictionary<string, object> values)
    {
        _db.Add(TableName, values);
    }

    public virtual void Update(T oldObj, T newObj)
    {
        _db.UpdateRow(oldObj, newObj);
    }

    public virtual void Delete(T obj)
    {
        _db.DeleteRow(TableName, obj);
    }

    public virtual List<ColumnInfo> GetColumnsInfo()
    {
        return _db.ListColumns(TableName);
    }

    public virtual List<T> GetAllData()
    {
        return _db.Find<T>();
    }
}