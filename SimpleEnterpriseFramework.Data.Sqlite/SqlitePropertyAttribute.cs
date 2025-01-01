namespace SimpleEnterpriseFramework.Data.Sqlite;

[AttributeUsage(AttributeTargets.Property)]
public class SqlitePropertyAttribute : Attribute
{
    public string Type { get; }
    public string Name { get; }
    public bool Unique { get; set; } = false;
    public bool IsKey { get; set; } = false;
    public bool Nullable { get; set; } = true;
    public bool Autoincrement { get; set; } = false;
    public (string table, string field)? ForeignConstraint { get; set; }
    static string[] allTypes = { "TEXT", "INTEGER", "REAL", "BLOB", "ANY" };

    public SqlitePropertyAttribute(string type, string name)
    {
        Type = type;
        Name = name;
        if (!allTypes.Contains(type))
        {
            throw new ArgumentException(
                $"Type {type} is not supported. Supported types are: {string.Join(", ", allTypes)}"
            );
        }
    }

    public SqlitePropertyAttribute(
        string type, string name, string referenceTable, string referenceField
    )
    {
        Type = type;
        Name = name;
        ForeignConstraint = (referenceTable, referenceField);

        if (!allTypes.Contains(type))
        {
            throw new ArgumentException(
                $"Type {type} is not supported. Supported types are: {string.Join(", ", allTypes)}"
            );
        }
    }

    public override string ToString() =>
        $"[Type = {Type}, Unique = {Unique.ToString()}, IsKey = {IsKey.ToString()}]";
}