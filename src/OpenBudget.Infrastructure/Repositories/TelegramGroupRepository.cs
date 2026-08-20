using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Interfaces;
using OpenBudget.Infrastructure.Data;

namespace OpenBudget.Infrastructure.Repositories;

public class TelegramGroupRepository : ITelegramGroupRepository
{
    private readonly AppDbContext _context;

    public TelegramGroupRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TelegramGroup?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return _context.TelegramGroups.FirstOrDefaultAsync(x => x.ChatId == chatId, cancellationToken);
    }

    public Task<List<TelegramGroup>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return _context.TelegramGroups.Where(x => x.IsActive).OrderByDescending(x => x.JoinedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TelegramGroup group, CancellationToken cancellationToken = default)
    {
        await _context.TelegramGroups.AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TelegramGroup group, CancellationToken cancellationToken = default)
    {
        _context.TelegramGroups.Update(group);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
