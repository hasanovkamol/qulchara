using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;
using OpenBudget.Domain.Interfaces;
using OpenBudget.Infrastructure.Data;

namespace OpenBudget.Infrastructure.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly AppDbContext _context;

    public VoteRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Vote?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Votes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Vote?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return _context.Votes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<(List<Vote> Items, int TotalCount)> GetByBrokerIdPagedAsync(int brokerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Votes.Where(x => x.BrokerId == brokerId);
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.VotedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<Vote> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Votes.Include(v => v.Broker);
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.VotedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Vote?> GetPendingVoteToConfirmAsync(string lastNDigits, DateTime targetTime, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        // Vaqt oralig'i: kiritilgan vaqtdan -5 soat oldindan to kiritilgan/hozirgi vaqtgacha
        var minTime = targetTime.AddHours(-5);
        var now = OpenBudget.Domain.Helpers.DateTimeHelper.UzbekistanNow;
        var maxTime = targetTime > now ? targetTime : now;

        return _context.Votes
            .Where(x => x.Status == VoteStatus.Pending 
                        && x.PhoneNumber.EndsWith(lastNDigits)
                        && x.VotedAt >= minTime 
                        && x.VotedAt.AddMinutes(-5) <= maxTime)
            .OrderBy(x => x.VotedAt) // Birinchi insert bo'yicha confirm qilinadi (ASC)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        await _context.Votes.AddAsync(vote, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        _context.Votes.Update(vote);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return _context.Votes.CountAsync(cancellationToken);
    }

    public Task<int> GetCountByStatusAsync(VoteStatus status, CancellationToken cancellationToken = default)
    {
        return _context.Votes.CountAsync(x => x.Status == status, cancellationToken);
    }

    public Task<int> GetCountByBrokerAndStatusAsync(int brokerId, VoteStatus status, CancellationToken cancellationToken = default)
    {
        return _context.Votes.CountAsync(x => x.BrokerId == brokerId && x.Status == status, cancellationToken);
    }
}
