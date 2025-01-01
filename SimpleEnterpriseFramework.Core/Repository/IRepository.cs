namespace SEF.Repository;
using System.Text;
using System.Data.Common;

public interface IRepository {
    public abstract List<String> ListTables();
    public abstract List<ColumnInfo> ListColumns(string table);
    public abstract void CreateTable<T>(bool deleteIfExist = false);

    // DeleteRow("User", "Id = 1 AND (Name = 'tuong' OR Name = '123')")
    public abstract void DeleteRow(string table, string condition);
    public abstract void DeleteRow(string table, object condition);

    // UpdateRow("User", "")
    public abstract void UpdateRow(string table, string condition, string updateStatement);
    public abstract void UpdateRow(object? conditions, object updates);

    public abstract void Add(string table, object values);
    public abstract void Add(object obj);

    public abstract List<object[]> Find(string table, object? conditions = null);
    public abstract List<T> Find<T>(object? conditions = null) where T: class, new();

    public abstract object[]? FindOne(string table, object conditions);
    public abstract T? FindOne<T>(object condition) where T: class, new();

    public abstract string GetPrimaryColumn(string table);
}

public struct ColumnInfo {
    public Int64 id;
    public string name;
    public string type;
    public bool nullable;
    public bool isPrimaryKey;
    public ColumnInfo(Int64 id, String name, String type, bool nullable, bool isPrimaryKey) {
        this.id = id;
        this.name = name;
        this.type = type;
        this.nullable = nullable;
        this.isPrimaryKey = isPrimaryKey;
    }
    public override string? ToString() => $"{id}:{type}:{name}:{(nullable ? "nullable" : "not nullable")}:{(isPrimaryKey ? "primary key" : "not primary key")}";
}