using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.DTOs;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Application.Services;

public interface IVoteService
{
    Task<(bool Success, string Message)> AddVoteAsync(int brokerId, string rawPhoneNumber, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ConfirmVoteAsync(int adminId, string lastNDigits, DateTime targetTime, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<PaginatedResult<VoteDto>> GetBrokerVotesPagedAsync(int brokerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BrokerStatsDto> GetBrokerStatsAsync(int brokerId, CancellationToken cancellationToken = default);
}
