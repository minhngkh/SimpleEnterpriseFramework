using SimpleEnterpriseFramework.Abstractions.App;

namespace SimpleEnterpriseFramework.Abstractions.Data;

public interface IDatabaseDriver {
    public List<String> ListTables();
    public List<ColumnInfo> ListColumns(string table);
    public void CreateTable<T>(bool deleteIfExist = false) where T : Model, new();

    // DeleteRow("User", "Id = 1 AND (Name = 'tuong' OR Name = '123')")

    public void Delete<T>(T obj, List<string>? selectedFields = null)
        where T : Model, new();
    public void DeleteRow(string table, string condition);
    public void DeleteRow(string table, object condition);

    public void Update<T>(
        T updates, List<string>? updateFields, T conditions, List<string>? conditionFields
    ) where T : Model, new();
    public void UpdateRow(string table, string condition, string updateStatement);
    public void UpdateRow(string table, object? conditions, object updates);
    public void UpdateRow(object? conditions, object updates);

    public void Add<T>(T obj, List<string>? selectedFields = null) where T : Model, new();
    public void Add(string table, object values);
    public void Add(object obj);

    public List<T> Find<T>(T? obj, List<string>? selectedFields = null) where T : Model, new();
    public List<object[]> Find(string table, object? conditions = null);
    public List<T> FindV0<T>(object? conditions = null) where T: Model, new();

    public T? First<T>(T? obj, List<string>? selectedFields = null) where T : Model, new();
    public object[]? FindOne(string table, object conditions);
    public T? FindOne<T>(object condition) where T: Model, new();

    public string GetPrimaryColumn(string table);
    
    public List<ModelFieldInfo> GetFields<T>(T obj) where T : Model, new();
}