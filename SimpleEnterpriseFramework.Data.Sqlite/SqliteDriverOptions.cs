namespace SimpleEnterpriseFramework.Data.Sqlite;

public class SqliteDriverOptions
{
    public string ConnectionString { get; set; } = "";

    public void UsePath(string path)
    {
        ConnectionString = $"Data Source={path}";
    }
}