using System.Security.Cryptography;

namespace SimpleEnterpriseFramework.Membership;


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
public class MembershipRepository
{
    IRepository _repository = new SqliteRepository("Data Source=new.db");
    PasswordHasher _hasher = new PasswordHasher();
    public void CreateUserTable()
    {
        // Create user table by calling repository
        try
        {
            _repository.CreateTable<User>();
            Console.WriteLine("User table created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating the user table: {ex.Message}");
        }
    }

    public void CreateRoleTable()
    {
        try
        {
            _repository.CreateTable<Role>();
            Console.WriteLine("Role table created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating the role table: {ex.Message}");
        }
    }

    public void CreateKeyTable()
    {

    }

    public void AddUser(String userName, String password, String roleName)
    {
        List<Role> roles =  _repository.Find<Role>(new {Name = roleName});
        Console.WriteLine(roleName);
        if (roles.Count == 0)
        {
            Console.WriteLine("Role name does not exist.");
            return;
        }
        try
        {
            _repository.Add(new User()
            {
                Username = userName,
                Password = _hasher.HashPassword(password),
                RoleId = roles[0].Id,
            });
            Console.WriteLine("User added successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating the user table: {ex.Message}");
        }
    }

    public void AddRole(String roleName)
    {
        List<Role> roles =  _repository.Find<Role>(new {Name = roleName});
        if (roles.Count != 0)
        {
            Console.WriteLine("Role name exists.");
        }
        try
        {
            _repository.Add(new Role()
            {
                Name = roleName,
            });
            Console.WriteLine("Role added successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating the role table: {ex.Message}");
        }
    }

    public void DeleteUser(String userName)
    {
        List<User> users = _repository.Find<User>(new { Username = userName });
        if (users.Count == 0)
        {
            Console.WriteLine("User not found.");
            return;
        }
        try
        {
            _repository.DeleteRow("User", new { Username = userName });
            Console.WriteLine(users[0].Username + " deleted successfully.");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Could not delete user.: {ex.Message}");
        }
    }
    

    public void DeleteRole(String roleName)
    {
        List<Role> roles = _repository.Find<Role>(new { Name = roleName });
        if (roles.Count == 0)
        {
            Console.WriteLine("Role not found.");
            return;
        }
        try
        {
            _repository.DeleteRow("Role", new { Name = roleName });
            Console.WriteLine(roles[0].Name + " deleted successfully.");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Could not delete role.: {ex.Message}");
        }
    }

    public void modifyUser(String userNameIdentify, String userName, String password, String roleName)
    {
        List<Role> roles = _repository.Find<Role>(new { Name = roleName });
        if (roles.Count == 0)
        {
            Console.WriteLine("Role not found.");
            return;
        }

        try
        {
            _repository.UpdateRow("User", new { Username = userNameIdentify },
                new
                {
                    Username = userName,
                    Password = _hasher.HashPassword(password),
                    RoleId = roles[0].Id,
                });

            Console.WriteLine("User updated successfully.");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Could not modify user.: {ex.Message}");
        }
    }
}