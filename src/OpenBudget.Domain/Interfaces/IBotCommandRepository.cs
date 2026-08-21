using OpenBudget.Domain.Entities;

namespace OpenBudget.Domain.Interfaces;

public interface IBotCommandRepository
{
    Task<(List<BotCommand> Commands, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BotCommand?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BotCommand?> GetByCommandTextAsync(string commandText, CancellationToken cancellationToken = default);
    Task UpdateAsync(BotCommand command, CancellationToken cancellationToken = default);
}
