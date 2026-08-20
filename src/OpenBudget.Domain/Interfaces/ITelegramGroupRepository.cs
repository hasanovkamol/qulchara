using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Domain.Interfaces;

public interface ITelegramGroupRepository
{
    Task<TelegramGroup?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default);
    Task<List<TelegramGroup>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TelegramGroup group, CancellationToken cancellationToken = default);
    Task UpdateAsync(TelegramGroup group, CancellationToken cancellationToken = default);
}
