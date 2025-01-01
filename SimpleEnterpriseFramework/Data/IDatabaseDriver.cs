namespace SimpleEnterpriseFramework.Data;

public interface IDatabaseDriver {
    public List<String> ListTables();
    public List<ColumnInfo> ListColumns(string table);
    public void CreateTable<T>(bool deleteIfExist = false);

    // DeleteRow("User", "Id = 1 AND (Name = 'tuong' OR Name = '123')")
    public void DeleteRow(string table, string condition);
    public void DeleteRow(string table, object condition);

    // UpdateRow("User", "")
    public void UpdateRow(string table, string condition, string updateStatement);
    public void UpdateRow(object? conditions, object updates);

    public void Add(string table, object values);
    public void Add(object obj);

    public List<object[]> Find(string table, object? conditions = null);
    public List<T> Find<T>(object? conditions = null) where T: class, new();

    public object[]? FindOne(string table, object conditions);
    public T? FindOne<T>(object condition) where T: class, new();

    public string GetPrimaryColumn(string table);
}