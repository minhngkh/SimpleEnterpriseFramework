using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SimpleEnterpriseFramework.Membership;
using SimpleEnterpriseFramework.WebApp;

namespace SimpleEnterpriseFramework.WebApp;

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

public class Membership
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private IRepository _repository;
    private PasswordHasher _passwordHasher;
    private readonly HashSet<string> _tokenBlacklist = new();
    private MembershipRepository _membershipRepository;
    
    public Membership(string secretKey, string issuer, string audience)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _repository = new SqliteRepository("Data Source=new.db");
        _passwordHasher = new PasswordHasher();
        _membershipRepository = new MembershipRepository();
    }
    public bool IsTokenBlacklisted(string token)
    {
        return _tokenBlacklist.Contains(token);
    }
    public String Login(String username, String password)
    {
        List<User> users = this._repository.Find<User>(new {Username = username});
        if (this._passwordHasher.VerifyPassword(password, users[0].Password))
        {
            return ""; // Unauthorized
        }
        
        var role = users[0].RoleId;
        List<Role> roles = this._repository.Find<Role>(new{Id = role });
        return GenerateToken(username, roles[0].Name);
    }
    public bool Register(string username, string password, string role = "User")
    {
        // Check if the username already exists
        if (!_membershipRepository.AddUser(username, password, role))
        {
            return false;
        }
        return true;
    }
    public void Logout(string token)
    {
        // Add token to the blacklist
        _tokenBlacklist.Add(token);
    }
    private string GenerateToken(string username, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}