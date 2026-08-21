using System.Collections.Generic;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Domain.Interfaces;

public interface IVoteConfirmationRepository
{
    Task<VoteConfirmation?> GetByIdAsync(long id);
    Task<List<VoteConfirmation>> GetPendingConfirmationsAsync();
    Task<bool> ExistsPendingConfirmationAsync(string lastNDigits, System.DateTime targetTime);
    Task<VoteConfirmation?> GetMatchingPendingConfirmationAsync(string lastNDigits);
    Task AddAsync(VoteConfirmation confirmation);
    Task UpdateAsync(VoteConfirmation confirmation);
    Task RejectExpiredConfirmationsAsync(System.DateTime expirationThreshold);
    Task<(List<VoteConfirmation> Items, int TotalCount)> GetHistoryPagedAsync(int page, int pageSize);
    Task<(List<VoteConfirmation> Items, int TotalCount)> GetBrokerHistoryPagedAsync(int brokerId, int page, int pageSize);
}
