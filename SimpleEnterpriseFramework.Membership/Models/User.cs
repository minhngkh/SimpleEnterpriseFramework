using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.Membership.Models;

public class User : Model
{
    [SqliteField("INTEGER", IsKey = true, Autoincrement = true)]
    public long Id;

    [SqliteField("TEXT", Unique = true, Nullable = false)]
    public string Username;

    [SqliteField("TEXT", Nullable = false)]
    public string Password;

    [SqliteField("INTEGER", "_sef_role", "Id", Nullable = true)]
    public long? RoleId;

    public override string TableName => "_sef_user";
}