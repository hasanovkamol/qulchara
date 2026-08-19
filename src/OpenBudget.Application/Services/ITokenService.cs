using OpenBudget.Domain.Entities;

namespace OpenBudget.Application.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
