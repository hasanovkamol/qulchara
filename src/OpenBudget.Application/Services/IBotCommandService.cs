using OpenBudget.Domain.Entities;

namespace OpenBudget.Application.Services;

public interface IBotCommandService
{
    Task<(List<BotCommand> Commands, int TotalCount)> GetCommandsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BotCommand?> GetCommandByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ToggleCommandStatusAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsCommandActiveAsync(string commandText, CancellationToken cancellationToken = default);
}
