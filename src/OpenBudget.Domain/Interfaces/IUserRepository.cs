using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<int> GetCountByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}
