using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenBudget.Application.Services;

public interface INotificationService
{
    Task NotifyBrokerVoteConfirmedAsync(long brokerTelegramId, string phoneNumber, CancellationToken cancellationToken = default);
    Task NotifyErrorAsync(Exception ex, long? userId = null, string? context = null, CancellationToken cancellationToken = default);
    Task NotifyInfoAsync(string message, CancellationToken cancellationToken = default);
}
