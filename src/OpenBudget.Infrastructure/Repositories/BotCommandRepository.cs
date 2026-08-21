using Microsoft.EntityFrameworkCore;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Interfaces;
using OpenBudget.Infrastructure.Data;

namespace OpenBudget.Infrastructure.Repositories;

public class BotCommandRepository : IBotCommandRepository
{
    private readonly AppDbContext _context;

    public BotCommandRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<BotCommand> Commands, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var total = await _context.BotCommands.CountAsync(cancellationToken);
        
        var items = await _context.BotCommands
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<BotCommand?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.BotCommands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<BotCommand?> GetByCommandTextAsync(string commandText, CancellationToken cancellationToken = default)
    {
        return await _context.BotCommands
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CommandText == commandText, cancellationToken);
    }

    public async Task UpdateAsync(BotCommand command, CancellationToken cancellationToken = default)
    {
        _context.BotCommands.Update(command);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
