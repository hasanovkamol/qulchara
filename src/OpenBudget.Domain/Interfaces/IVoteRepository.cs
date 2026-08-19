using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Domain.Interfaces;

public interface IVoteRepository
{
    Task<Vote?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Vote?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    
    // For pagination (Broker's view)
    Task<(List<Vote> Items, int TotalCount)> GetByBrokerIdPagedAsync(int brokerId, int page, int pageSize, CancellationToken cancellationToken = default);
    
    // For Admin confirmation
    Task<Vote?> GetPendingVoteToConfirmAsync(string last3Digits, DateTime targetTime, TimeSpan timeWindow, CancellationToken cancellationToken = default);

    Task AddAsync(Vote vote, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default);

    // Stats
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByStatusAsync(VoteStatus status, CancellationToken cancellationToken = default);
    Task<int> GetCountByBrokerAndStatusAsync(int brokerId, VoteStatus status, CancellationToken cancellationToken = default);
}
