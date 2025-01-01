namespace SimpleEnterpriseFramework.Data.Sqlite;

public class SqliteDataType(string value)
{
    public string Value { get; } = value;

    public static string Text => new("TEXT");
    public static string Integer => new("INTEGER");
    public static string Real => new("REAL");
    public static string Blob => new("BLOB");
    public static string Any => new("ANY");

    public override string ToString()
    {
        return value;
    }
}