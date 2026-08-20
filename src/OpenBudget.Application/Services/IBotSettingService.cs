using System.Threading;
using System.Threading.Tasks;

namespace OpenBudget.Application.Services;

public interface IBotSettingService
{
    Task<int> GetLastDigitsCountAsync(CancellationToken cancellationToken = default);
    Task<bool> SetLastDigitsCountAsync(int count, CancellationToken cancellationToken = default);

    Task<bool> GetAllowGuestRegistrationAsync(CancellationToken cancellationToken = default);
    Task<bool> SetAllowGuestRegistrationAsync(bool allow, CancellationToken cancellationToken = default);
}
