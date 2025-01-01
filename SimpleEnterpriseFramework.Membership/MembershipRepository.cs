using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.Membership;

class Role
{
    [SqliteField("INTEGER", Unique = true, IsKey = true)]
    public int Id;

    [SqliteField("TEXT", Unique = true, Nullable = false)]
    public string Name;
}

class User
{
    [SqliteField("INTEGER", Unique = true, IsKey = true)]
    public int Id;

    [SqliteField("TEXT", Unique = true, Nullable = false)]
    public string Username;

    [SqliteField("TEXT", Nullable = false)] public string Password;

    [SqliteField("INTEGER", "Role", "Id", Nullable = false)]
    public int RoleId;
}

public class MembershipRepository(IDatabaseDriver db)
{
    PasswordHasher _hasher = new PasswordHasher();

    public void CreateUserTable()
    {
        // Create user table by calling repository
        try
        {
            db.CreateTable<User>();
            Console.WriteLine("User table created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"An error occurred while creating the user table: {ex.Message}");
        }
    }

    public void CreateRoleTable()
    {
        try
        {
            db.CreateTable<Role>();
            Console.WriteLine("Role table created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"An error occurred while creating the role table: {ex.Message}");
        }
    }

    public void CreateKeyTable()
    {
    }

    public bool AddUser(String userName, String password, String roleName)
    {
        List<Role> roles = db.Find<Role>(new { Name = roleName });
        Console.WriteLine(roleName);
        if (roles.Count == 0)
        {
            Console.WriteLine("Role name does not exist.");
            return false;
        }

        try
        {
            db.Add(new User()
            {
                Username = userName,
                Password = _hasher.HashPassword(password),
                RoleId = roles[0].Id,
            });
            Console.WriteLine("User added successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"An error occurred while creating the user table: {ex.Message}");
        }

        return true;
    }

    public void AddRole(String roleName)
    {
        List<Role> roles = db.Find<Role>(new { Name = roleName });
        if (roles.Count != 0)
        {
            Console.WriteLine("Role name exists.");
        }

        try
        {
            db.Add(new Role()
            {
                Name = roleName,
            });
            Console.WriteLine("Role added successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"An error occurred while creating the role table: {ex.Message}");
        }
    }

    public void DeleteUser(String userName)
    {
        List<User> users = db.Find<User>(new { Username = userName });
        if (users.Count == 0)
        {
            Console.WriteLine("User not found.");
            return;
        }

        try
        {
            db.DeleteRow("User", new { Username = userName });
            Console.WriteLine(users[0].Username + " deleted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not delete user.: {ex.Message}");
        }
    }


    public void DeleteRole(String roleName)
    {
        List<Role> roles = db.Find<Role>(new { Name = roleName });
        if (roles.Count == 0)
        {
            Console.WriteLine("Role not found.");
            return;
        }

        try
        {
            db.DeleteRow("Role", new { Name = roleName });
            Console.WriteLine(roles[0].Name + " deleted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not delete role.: {ex.Message}");
        }
    }

    public void modifyUser(String userNameIdentify, String userName, String password,
        String roleName)
    {
        List<Role> roles = db.Find<Role>(new { Name = roleName });
        if (roles.Count == 0)
        {
            Console.WriteLine("Role not found.");
            return;
        }

        try
        {
            db.UpdateRow("User", new { Username = userNameIdentify },
                new
                {
                    Username = userName,
                    Password = _hasher.HashPassword(password),
                    RoleId = roles[0].Id,
                });

            Console.WriteLine("User updated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not modify user.: {ex.Message}");
        }
    }
}