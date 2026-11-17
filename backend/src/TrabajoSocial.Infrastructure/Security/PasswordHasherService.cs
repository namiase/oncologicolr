using Microsoft.AspNetCore.Identity;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Infrastructure.Security;

/// <summary>PBKDF2 (HMAC-SHA512, 100.000 iteraciones) mediante PasswordHasher de ASP.NET Core Identity.</summary>
internal sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> hasher = new();

    public string Hash(User user, string password) => hasher.HashPassword(user, password);

    public PasswordCheck Verify(User user, string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return PasswordCheck.Failed;
        }

        return hasher.VerifyHashedPassword(user, user.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
            _ => PasswordCheck.Failed
        };
    }
}
