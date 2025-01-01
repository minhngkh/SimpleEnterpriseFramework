using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SimpleEnterpriseFramework.Abstractions;
using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.Membership;

public class Membership(IDatabaseDriver db, MembershipRepository repo, MembershipOptions options) : IMembership
{
    // private readonly string _issuer;
    // private readonly string _audience;
    private PasswordHasher _passwordHasher = new();
    private readonly HashSet<string> _tokenBlacklist = [];


    public bool IsTokenBlacklisted(string token)
    {
        return _tokenBlacklist.Contains(token);
    }

    public String Login(String username, String password)
    {
        List<User> users = db.Find<User>(new { Username = username });
        if (this._passwordHasher.VerifyPassword(password, users[0].Password))
        {
            return ""; // Unauthorized
        }

        var role = users[0].RoleId;
        List<Role> roles = db.Find<Role>(new { Id = role });
        return GenerateToken(username, roles[0].Name);
    }

    public bool Register(string username, string password, string role = "User")
    {
        // Check if the username already exists
        if (!repo.AddUser(username, password, role))
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
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var credentials =
            new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
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