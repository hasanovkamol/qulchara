using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Domain.Interfaces;

public interface IBotSettingRepository
{
    Task<BotSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default);
}
