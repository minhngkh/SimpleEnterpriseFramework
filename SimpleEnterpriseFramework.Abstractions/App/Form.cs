using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.Abstractions.App;

public abstract class Form<T>
    where T : Model, new()
{
    private readonly IDatabaseDriver _db;
    private Dictionary<string, string> _aliasMap;
    private List<string> _fields;

    public Form(IDatabaseDriver db)
    {
        _db = db;
        _aliasMap = _db.GetFields(new T()).ToDictionary(f => f.Name, f => f.Alias);
        _fields = _aliasMap.Keys.ToList();
    }

    private List<string>? GetSellectedFields(List<string>? ignoredFields)
    {
        if (ignoredFields == null)
        {
            return null;
        }

        return _fields.Except(ignoredFields).Select(f => _aliasMap[f]).ToList();
    }


    public string TableName { get; } = ModelHelpers.GetTableName<T>();

    public void Add(T obj, List<string>? ignoredFields = null)
    {
        _db.Add(obj, GetSellectedFields(ignoredFields));
    }

    public void Add(Dictionary<string, object> values)
    {
        _db.Add(TableName, values);
    }

    public void Update(T oldObj, T newObj)
    {
        _db.UpdateRow(oldObj, newObj);
    }

    public void Delete(T obj)
    {
        _db.DeleteRow(TableName, obj);
    }

    public List<ColumnInfo> GetColumnsInfo()
    {
        return _db.ListColumns(TableName);
    }

    // TODO: update this table name
    public List<T> GetAllData()
    {
        return _db.Find<T>(null);
    }

    public List<List<ModelFieldInfo>> GetFields()
    {
        return _db.Find<T>(null).Select(_db.GetFields).ToList();
    }

    public string GetAlias(string fieldName)
    {
        return _aliasMap[fieldName];
    }
}