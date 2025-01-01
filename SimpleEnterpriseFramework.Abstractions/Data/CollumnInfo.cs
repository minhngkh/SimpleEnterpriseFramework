namespace SimpleEnterpriseFramework.Abstractions.Data;

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