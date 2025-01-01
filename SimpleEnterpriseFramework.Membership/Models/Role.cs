using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.Membership.Models;

public class Role : Model
{
    [SqliteProperty("INTEGER", "id", IsKey = true, Autoincrement = true)]
    public long Id { get; set; }

    [SqliteProperty("TEXT", "name", Unique = true, Nullable = false)]
    public string Name { get; set; }

    public override string TableName => "_sef_role";
}