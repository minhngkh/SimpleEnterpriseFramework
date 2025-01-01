using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SimpleEnterpriseFramework.Abstractions;
using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.Membership;

public class Membership : IMembership
{
    private readonly IDatabaseDriver _db;
    private readonly MembershipRepository _repo;
    private readonly MembershipOptions _options;
    private readonly PasswordHasher _passwordHasher;
    private readonly HashSet<string> _tokenBlacklist = [];

    public Membership(
        IDatabaseDriver db, MembershipRepository repo, MembershipOptions options
    )
    {
        _db = db;
        _repo = repo;
        _options = options;
        _passwordHasher = new PasswordHasher();
    }

    public void Setup(bool toReset = false) 
    {
        _repo.CreateRoleTable(toReset);
        _repo.CreateUserTable(toReset);
        
        _repo.AddRole("member");
        _repo.AddRole("admin");
    }

    // TODO: store the token in db instead of the memory
    public string ChangePassword(string username, string newPassword, string token)
    {
        throw new NotImplementedException();
    }

    public bool IsLoggedIn(string token)
    {
        return !_tokenBlacklist.Contains(token);
    }

    public bool Login(string username, string password,
        [MaybeNullWhen(false)] out string token)
    {
        try
        {
            var user = _repo.GetUserByUsername(username);

            // user not found or password is incorrect
            if (user == null)
            {
                Console.WriteLine("User not found.");
                token = default;
                return false;
            }
            
            Console.WriteLine(user);
            
            if (!BCrypt.Net.BCrypt.EnhancedVerify(password, user.Password))
            {
                Console.WriteLine("Password is incorrect.");
                token = default;
                return false;
            }

            token = GenerateToken(username, user.RoleId);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex}");
            token = default;
            return false;
        }
    }

    public bool Register(string username, string password, string? role = default)
    {
        // Check if the username already exists
        // if (!_repo.AddUser(username, password, role))
        // {
        //     return false;
        // }

        // var user = _repo.GetUserByUsername(username);

        try
        {
            var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
            _repo.AddUser(username, hashedPassword, role);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register error: {ex.Message}");
            return false;
        }

        return true;
    }

    public string RecoverPassword(string username)
    {
        throw new NotImplementedException();
    }

    public void Logout(string token)
    {
        // Add token to the blacklist
        _tokenBlacklist.Add(token);
    }

    private string GenerateToken(string username, long? roleId)
    {
        var securityKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials =
            new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("user_username", username),
            new Claim("user_roleId", roleId.ToString() ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            // issuer: _issuer,
            // audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}