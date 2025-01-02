using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Membership.Models;

namespace SimpleEnterpriseFramework.Membership;

public class MembershipRepository(IDatabaseDriver db)
{
    PasswordHasher _hasher = new PasswordHasher();

    public void CreateUserTable(bool toReset = false)
    {
        // Create user table by calling repository
        try
        {
            db.CreateTable<User>(toReset);
            Console.WriteLine("User table created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"An error occurred while creating the user table: {ex.Message}");
        }
    }

    public void CreateRoleTable(bool toReset = false)
    {
        try
        {
            db.CreateTable<Role>(toReset);
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

    public void AddUser(string userName, string password, string? roleName = default)
    {
        if (roleName != null)
        {
            var role = db.First(new Role { Name = roleName }, ["Name"]);
            if (role == null) throw new Exception("Role not found.");
            db.Add(
                new User
                {
                    Username = userName,
                    Password = password,
                    RoleId = role.Id
                },
                ["Username", "Password", "RoleId"]
            );
        }
        else
        {
            db.Add(
                new User
                {
                    Username = userName,
                    Password = password
                },
                ["Username", "Password"]
            );
        }

        Console.WriteLine("User added successfully.");
    }

    public void AddRole(string roleName)
    {
        var roles = db.FindV0<Role>(new { Name = roleName });
        if (roles.Count != 0)
        {
            Console.WriteLine("Role name exists.");
        }

        try
        {
            db.Add(
                new Role
                {
                    Name = roleName
                },
                ["Name"]
            );
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
        List<User> users = db.FindV0<User>(new { Username = userName });
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
        List<Role> roles = db.FindV0<Role>(new { Name = roleName });
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
        List<Role> roles = db.FindV0<Role>(new { Name = roleName });
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

    public (User?, Role?) GetUserByUsername(string username)
    {
        var user = db.First(
            new User { Username = username },
            ["Username"]
        );
        if (user != null && user.RoleId != null) {
            var role = db.First(
                new Role { Id = user.RoleId ?? 0 },
                ["Id"]
            );
            return (user, role);
        } else {
            return (user, null);
        }
    }
}