using SEF.UI;
using SEF.Repository;

public class User {
#pragma warning disable 0414
    public string username = "";
    public string email = "";
    public string phone = "";
    public string password = "";
#pragma warning restore 0414
}

public class UserForm : UIForm<User> {
    public UserForm(IRepository repo): base(repo) {
    }
}
