using System.Diagnostics.CodeAnalysis;

namespace SimpleEnterpriseFramework.Abstractions;

public interface IMembership
{
    /**
     * Set up the all the necessary tables for the framework to work
     */
    void Setup();

    bool Login(string username, string password, [MaybeNullWhen(false)] out string token);

    bool Register(string username, string password, string? role = default);

    string RecoverPassword(string username);

    string ChangePassword(string username, string newPassword, string token);

    bool IsLoggedIn(string token);

    void Logout(string token);
}