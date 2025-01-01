using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.Membership.Models;

public class User : Model
{
    [SqliteProperty("INTEGER", "id", IsKey = true, Autoincrement = true)]
    public long Id { get; set; }

    [SqliteProperty("TEXT", "username", Unique = true, Nullable = false)]
    public string Username { get; set; }

    [SqliteProperty("TEXT", "password", Nullable = false)]
    public string Password { get; set; }

    [SqliteProperty("INTEGER", "role_id", "_sef_role", "Id", Nullable = true)]
    public long? RoleId {get; set;}

    public override string TableName => "_sef_user";
}