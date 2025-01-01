using SimpleEnterpriseFramework.Abstractions.App;
using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.AppDemo.Models;

public class User : Model
{
    [SqliteProperty("INTEGER", "id", IsKey = true, Autoincrement = true)]
    public long Id { get; set; }

    [SqliteProperty("TEXT", "username", Nullable = false)]
    public string Username { get; set; }

    [SqliteProperty("TEXT", "email", Nullable = false)]
    public string Email { get; set; }

    [SqliteProperty("TEXT", "phone")]
    public string? Phone { get; set; }

    [SqliteProperty("TEXT", "password", Nullable = false)]
    public string Password { get; set; }

    public override string TableName => "user";
}

public class UserForm(IDatabaseDriver db) : Form<User>(db)
{
}