namespace SimpleEnterpriseFramework.Data.Sqlite;

[AttributeUsage(AttributeTargets.Field)]
public class SqliteFieldAttribute : Attribute
{
    public string Type;
    public string Name;
    public bool Unique = false;
    public bool IsKey = false;
    public bool Nullable = true;
    public bool Autoincrement = false;
    public (string table, string field)? ForeignConstraint = null;
    static string[] allTypes = { "TEXT", "INTEGER", "REAL", "BLOB", "ANY" };

    public SqliteFieldAttribute(string type, string name)
    {
        this.Type = type;
        this.Name = name;
        if (!allTypes.Contains(type.ToUpper()))
        {
            throw new Exception(
                $"Invalid SqliteType {type}, must be one of the following: TEXT, INTEGER, REAL, BLOB, ANY.");
        }
    }

    public SqliteFieldAttribute(string type, string name, string referencedTable, string referencedField)
    {
        this.Name = name;
        this.ForeignConstraint = (referencedTable, referencedField);
        this.Type = type;
        if (!allTypes.Contains(type.ToUpper()))
        {
            throw new Exception(
                $"Invalid SqliteType {type}, must be one of the following: TEXT, INTEGER, REAL, BLOB, ANY.");
        }
    }

    public override string ToString() =>
        $"[Type = {Type}, Unique = {Unique.ToString()}, IsKey = {IsKey.ToString()}]";
}