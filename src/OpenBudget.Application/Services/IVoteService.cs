using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.DTOs;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Application.Services;

public interface IVoteService
{
    Task<(bool Success, string Message)> AddVoteAsync(int brokerId, string rawPhoneNumber, DateTime votedAt, CancellationToken cancellationToken = default);
    Task MatchPendingVotesAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ConfirmVoteAsync(int adminId, string lastNDigits, DateTime targetTime, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<PaginatedResult<VoteDto>> GetBrokerVotesPagedAsync(int brokerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedResult<VoteDto>> GetAllVotesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BrokerStatsDto> GetBrokerStatsAsync(int brokerId, CancellationToken cancellationToken = default);
    Task<List<OpenBudget.Domain.Entities.VoteConfirmation>> GetPendingConfirmationsAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResult<OpenBudget.Application.DTOs.VoteConfirmationDto>> GetConfirmationHistoryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedResult<OpenBudget.Application.DTOs.VoteConfirmationDto>> GetBrokerConfirmationHistoryPagedAsync(int brokerId, int page, int pageSize, CancellationToken cancellationToken = default);
}
