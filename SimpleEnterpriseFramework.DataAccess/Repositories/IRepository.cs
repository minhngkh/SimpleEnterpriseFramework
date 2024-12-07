using System.Text;
using System.Data.Common;
using Microsoft.Data.Sqlite;

public interface IRepository {
    public abstract List<String> ListTables();
    public abstract List<ColumnInfo> ListColumns(string table);
    public abstract void CreateTable<T>(bool deleteIfExist = false) where T: struct;

    // DeleteRow("User", "Id = 1 AND (Name = 'tuong' OR Name = '123')")
    public abstract void DeleteRow(string table, string condition);
    public abstract void DeleteRow(string table, object condition);

    // UpdateRow("User", "")
    public abstract void UpdateRow(string table, string condition, string updateStatement);
    public abstract void UpdateRow(string table, object? conditions, object updates);

    public abstract void Add(string table, object values);
    public abstract void Add<T>(T obj);

    public abstract List<object[]> Find(string table, object? conditions = null);
    public abstract List<T> Find<T>(object? conditions = null) where T: struct;

    public abstract object[]? FindOne(string table, object conditions);
    public abstract T? FindOne<T>(object condition) where T: struct;
}

public struct ColumnInfo {
    public Int64 id;
    public string name;
    public string type;
    public ColumnInfo(Int64 id, String name, String type) {
        this.id = id;
        this.name = name;
        this.type = type;
    }
    public override string? ToString() => $"{id}:{type}:{name}";
}