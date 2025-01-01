namespace SimpleEnterpriseFramework.Data.Sqlite;

[AttributeUsage(AttributeTargets.Field)]
public class SqliteFieldAttribute : Attribute
{
    public string Type;
    public bool Unique = false;
    public bool IsKey = false;
    public bool Nullable = true;
    public bool Autoincrement = false;
    public (string, string)? ForeignConstraint = null;
    static string[] allTypes = { "TEXT", "INTEGER", "REAL", "BLOB", "ANY" };

    public SqliteFieldAttribute(string type)
    {
        this.Type = type;
        if (!allTypes.Contains(type.ToUpper()))
        {
            throw new Exception(
                $"Invalid SqliteType {type}, must be one of the following: TEXT, INTEGER, REAL, BLOB, ANY.");
        }
    }

    public SqliteFieldAttribute(string type, string referencedTable, string referencedField)
    {
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