using SEF.UI;
using SEF.Repository;

SqliteRepository repo = new("Data Source=test.db");
WebUI ui = new WebUI(repo);
ui.Init();
ui.Register<User, UserForm>(new UserForm(repo));
ui.Start();

class User {
    // [DbField("INTEGER", Unique = true, IsKey = true, Autoincrement = true)]
    // public Int64? Id = null;

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string username = "";

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string email = "";

    [DbField("TEXT")]
    public string? phone = null;

    [DbField("TEXT", Nullable = false)]
    public string password = "";

    public override string ToString() => $"[User] '{username}' '{email}' '{phone}' '{password}' '{null}'";
}

class UserForm: UIForm<User> {
    public UserForm(IRepository repo): base(repo) {
    }
}
