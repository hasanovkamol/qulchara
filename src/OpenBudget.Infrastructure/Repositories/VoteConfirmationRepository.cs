using Microsoft.EntityFrameworkCore;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;
using OpenBudget.Domain.Interfaces;
using OpenBudget.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBudget.Infrastructure.Repositories;

public class VoteConfirmationRepository : IVoteConfirmationRepository
{
    private readonly AppDbContext _context;

    public VoteConfirmationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VoteConfirmation?> GetByIdAsync(long id)
    {
        return await _context.VoteConfirmations.FindAsync(id);
    }

    public async Task<List<VoteConfirmation>> GetPendingConfirmationsAsync()
    {
        return await _context.VoteConfirmations
            .Where(vc => vc.Status == VoteConfirmationStatus.Pending)
            .ToListAsync();
    }

    public async Task<bool> ExistsPendingConfirmationAsync(string lastNDigits, DateTime targetTime)
    {
        return await _context.VoteConfirmations
            .AnyAsync(vc => vc.LastNDigits == lastNDigits && vc.TargetTime == targetTime && vc.Status != VoteConfirmationStatus.Rejected);
    }

    public async Task<VoteConfirmation?> GetMatchingPendingConfirmationAsync(string lastNDigits)
    {
        return await _context.VoteConfirmations
            .Where(vc => vc.Status == VoteConfirmationStatus.Pending && vc.LastNDigits == lastNDigits)
            .OrderBy(vc => vc.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(VoteConfirmation confirmation)
    {
        await _context.VoteConfirmations.AddAsync(confirmation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(VoteConfirmation confirmation)
    {
        _context.VoteConfirmations.Update(confirmation);
        await _context.SaveChangesAsync();
    }

    public async Task RejectExpiredConfirmationsAsync(DateTime expirationThreshold)
    {
        var expired = await _context.VoteConfirmations
            .Where(vc => vc.Status == VoteConfirmationStatus.Pending && vc.CreatedAt < expirationThreshold)
            .ToListAsync();

        if (expired.Any())
        {
            foreach (var item in expired)
            {
                item.Status = VoteConfirmationStatus.Rejected;
            }
            _context.VoteConfirmations.UpdateRange(expired);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<(List<VoteConfirmation> Items, int TotalCount)> GetHistoryPagedAsync(int page, int pageSize)
    {
        var query = _context.VoteConfirmations
            .Include(vc => vc.MatchedVote)
                .ThenInclude(v => v.Broker)
            .OrderByDescending(vc => vc.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<VoteConfirmation> Items, int TotalCount)> GetBrokerHistoryPagedAsync(int brokerId, int page, int pageSize)
    {
        var query = _context.VoteConfirmations
            .Include(vc => vc.MatchedVote)
                .ThenInclude(v => v.Broker)
            .Where(vc => vc.Status == VoteConfirmationStatus.Confirmed && vc.MatchedVote != null && vc.MatchedVote.BrokerId == brokerId)
            .OrderByDescending(vc => vc.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
