namespace Toptanci.Application.Common.Abstractions;

/// <summary>Parola özetleme ve doğrulama.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
