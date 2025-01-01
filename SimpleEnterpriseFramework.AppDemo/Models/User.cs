using SimpleEnterpriseFramework.Abstractions.App;
using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.AppDemo.Models;

public class User : Model {
    public Int64? Id = null;
    public string username = "";
    public string email = "";
    public string? phone = null;
    public string password = "";
    
    public override string TableName => "User";
}

public class UserForm(IDatabaseDriver db) : Form<User>(db)
{
}