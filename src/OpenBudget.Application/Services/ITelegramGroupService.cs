using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Application.Services;

public interface ITelegramGroupService
{
    Task<TelegramGroup> TrackGroupActivityAsync(long chatId, string title, string? username = null, CancellationToken cancellationToken = default);
    Task SetGroupInactiveAsync(long chatId, CancellationToken cancellationToken = default);
    Task<List<TelegramGroup>> GetActiveGroupsAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SetInitiativeCodeAsync(long chatId, string initiativeCode, CancellationToken cancellationToken = default);
    Task<List<TelegramGroup>> GetActiveGroupsWithCodeAsync(CancellationToken cancellationToken = default);
}
