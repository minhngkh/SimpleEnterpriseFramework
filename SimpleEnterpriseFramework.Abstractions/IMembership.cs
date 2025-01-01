namespace SimpleEnterpriseFramework.Abstractions;

public interface IMembership
{
    bool IsTokenBlacklisted(string token);
    String Login(String username, String password);
    bool Register(string username, string password, string role = "User");
    void Logout(string token);
}