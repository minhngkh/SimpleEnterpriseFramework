using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.Membership.Models;

public class Role : Model
{
    [SqliteField("INTEGER", Unique = true, IsKey = true)]
    public long Id;

    [SqliteField("TEXT", Unique = true, Nullable = false)]
    public string Name;

    public override string TableName => "_sef_role";
}