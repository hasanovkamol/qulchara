using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Helpers;
using OpenBudget.Domain.Interfaces;
using OpenBudget.Infrastructure.Data;

namespace OpenBudget.Infrastructure.Repositories;

public class BotSettingRepository : IBotSettingRepository
{
    private readonly AppDbContext _context;

    public BotSettingRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<BotSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return _context.BotSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public async Task SetValueAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default)
    {
        var setting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (setting == null)
        {
            setting = new BotSetting
            {
                Key = key,
                Value = value,
                Description = description,
                UpdatedAt = DateTimeHelper.UzbekistanNow
            };
            await _context.BotSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = value;
            if (description != null) setting.Description = description;
            setting.UpdatedAt = DateTimeHelper.UzbekistanNow;
            _context.BotSettings.Update(setting);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
