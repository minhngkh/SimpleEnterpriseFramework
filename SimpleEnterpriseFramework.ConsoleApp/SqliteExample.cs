using System.Diagnostics;
struct Role {
    [DbField("INTEGER", Unique = true, IsKey = true)]
    public int Id;

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string Name;
}

struct User {
    [DbField("INTEGER", Unique = true, IsKey = true)]
    public int Id;

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string Username;

    [DbField("TEXT", Nullable = false)]
    public string Password;

    [DbField("INTEGER", "Role", "Id", Nullable = false)]
    public int RoleId;
}

class SqliteExample {
    public static void Main(string[] args) {
        SqliteRepository repo = new("Data Source=test.db");

        repo.CreateTable<User>(true);
        repo.CreateTable<Role>(true);

        repo.Add(new Role() {
            Id = 0,
            Name = "admin",
        });
        repo.Add(new Role() {
            Id = 1,
            Name = "user",
        });
        repo.Add(new Role() {
            Id = 2,
            Name = "guest",
        });


        Console.WriteLine("\nAll Roles");
        foreach (Role r in repo.Find<Role>()) {
            Console.WriteLine($"Id = {r.Id}, Name = {r.Name}");
        }

        Console.WriteLine("\nDelete Role Id = 2");
        repo.DeleteRow("Role", new {
            Id = 2,
        });

        Console.WriteLine("\nAll Roles");
        foreach (Role r in repo.Find<Role>()) {
            Console.WriteLine($"Id = {r.Id}, Name = {r.Name}");
        }

        repo.Add(new User() {
            Id = 123,
            Username = "Tuong",
            Password = "123",
            RoleId = 0,
        });
        repo.Add(new User() {
            Id = 125,
            Username = "Tuong2",
            Password = "123",
            RoleId = 0,
        });

        Console.WriteLine("\nAll Columns");
        List<ColumnInfo> columns = repo.ListColumns("User");
        foreach (var col in columns) {
            Console.WriteLine($"{col.name} {col.type}");
        }

        Console.WriteLine("\nAll users");
        foreach (User usr in repo.Find<User>()) {
            Console.WriteLine($"Id = {usr.Id}, Username = {usr.Username}, Password = {usr.Password}");
        }

        Console.WriteLine("\nAll users");
        foreach (var row in repo.Find("User")) {
            foreach (var col in row) {
                Console.Write($"{col.ToString()}   ");
            }
            Console.WriteLine();
        }

        Console.WriteLine("\nFind Id = 123");
        List<User> users = repo.Find<User>(new {Id = 123});
        foreach (User usr in users) {
            Console.WriteLine($"Id = {usr.Id}, Username = {usr.Username}, Password = {usr.Password}");
        }

        Console.WriteLine("\nFindOne Id = 124");
        object[]? result = repo.FindOne("User", new {Id = 124});
        if (result != null) {
            foreach (object col in result ) {
                Console.Write($"{col.ToString()}   ");
            }
            Console.WriteLine();
        } else {
            Console.WriteLine("Not found");
        }

        Console.WriteLine("\nFindOne Id = 123");
        User? user = repo.FindOne<User>(new {Id = 123});
        Console.WriteLine($"Id = {user?.Id}, Username = {user?.Username}, Password = {user?.Password}");

        Console.WriteLine("\nUpdate Id = 125 -> 124");
        repo.UpdateRow("User", new {Id = 125}, new {Id = 124});

        Console.WriteLine("\nFindOne Id = 124");
        user = repo.FindOne<User>(new {Id = 124});
        Debug.Assert(user is not null);
        if (user is not null) {
            Console.WriteLine($"Id = {user?.Id}, Username = {user?.Username}, Password = {user?.Password}");
        } else {
            Console.WriteLine("Not found");
        }
    }
}