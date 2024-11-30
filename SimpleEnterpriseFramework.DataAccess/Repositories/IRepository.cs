using System.Text;
using System.Data.Common;
using Microsoft.Data.Sqlite;

public interface IRepository {
    public abstract List<String> ListTables();
    public abstract List<ColumnInfo> ListColumns(string table);
    public abstract void CreateTable<T>(bool deleteIfExist = false) where T: struct;

    // DeleteRow("User", "Id = 1 AND (Name = 'tuong' OR Name = '123')")
    public abstract void DeleteRow(string table, string condition);
    public abstract void DeleteRow(string table, params (string, object)[] andConditions);

    // UpdateRow("User", "")
    public abstract void UpdateRow(string table, string condition, string updateStatement);
    public abstract IUpdateCommandBuilder UpdateRowBuilder();

    public abstract void Add(string table, params (string, object)[] values);
    public abstract void Add<T>(T obj);

    // Find("User", "Username = 'tuong' AND Id = 1")
    public abstract List<object[]> Find(string table, string? condition);
    public abstract List<T> Find<T>(string? condition) where T: struct;

    public abstract List<object[]> Find(string table, params (string, object)[] andConditions);
    public abstract List<T> Find<T>(params (string, object)[] andConditions) where T: struct;

    public abstract object[]? FindOne(string table, params (string, object)[] andConditions);
    public abstract T? FindOne<T>(params (string, object)[] andConditions) where T: struct;
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
