namespace SimpleEnterpriseFramework.Data.Sqlite;

public class SqliteDriverOptions
{
    public string ConnectionString { get; set; } = "";

    public SqliteDriverOptions UsePath(string path)
    {
        ConnectionString = $"Data Source={path}";
        return this;
    }
}