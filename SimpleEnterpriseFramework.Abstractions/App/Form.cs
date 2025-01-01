using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.Abstractions.App;

public abstract class Form<T>(IDatabaseDriver db)
    where T : Model, new()
{
    public string TableName { get; } = ModelHelpers.GetTableName<T>();

    public void Add(T obj)
    {
        db.Add(obj);
    }

    public void Add(Dictionary<string, object> values)
    {
        db.Add(TableName, values);
    }

    public void Update(T oldObj, T newObj)
    {
        db.UpdateRow(oldObj, newObj);
    }

    public void Delete(T obj)
    {
        db.DeleteRow(TableName, obj);
    }

    public List<ColumnInfo> GetColumnsInfo()
    {
        return db.ListColumns(TableName);
    }

    // TODO: update this table name
    public List<T> GetAllData()
    {
        return db.Find<T>();
    }
}