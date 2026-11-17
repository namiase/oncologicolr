using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Common.Interfaces;

public enum PasswordCheck
{
    Failed,
    Success,
    SuccessRehashNeeded
}

public interface IPasswordHasherService
{
    string Hash(User user, string password);
    PasswordCheck Verify(User user, string password);
}
