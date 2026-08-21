using Microsoft.Extensions.Caching.Memory;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenBudget.Application.Services;

public class BotCommandService : IBotCommandService
{
    private readonly IBotCommandRepository _repository;
    private readonly IMemoryCache _cache;

    public BotCommandService(IBotCommandRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<(List<BotCommand> Commands, int TotalCount)> GetCommandsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _repository.GetPagedAsync(page, pageSize, cancellationToken);
    }

    public async Task<BotCommand?> GetCommandByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ToggleCommandStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var command = await _repository.GetByIdAsync(id, cancellationToken);
        if (command == null) return false;

        command.IsActive = !command.IsActive;
        await _repository.UpdateAsync(command, cancellationToken);

        // Keshni tozalaymiz
        string cacheKey = $"BotCommand_Active_{command.CommandText}";
        _cache.Remove(cacheKey);

        return command.IsActive;
    }

    public async Task<bool> IsCommandActiveAsync(string commandText, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"BotCommand_Active_{commandText}";

        // Keshda bormi?
        if (_cache.TryGetValue(cacheKey, out bool isCachedActive))
        {
            return isCachedActive;
        }

        // Keshda yo'q, DB dan izlaymiz
        var command = await _repository.GetByCommandTextAsync(commandText, cancellationToken);

        // Agar bunday komanda bazada bo'lmasa, demak uni bloklash kerak emas.
        if (command == null) 
        {
            // Buni keshlab qo'yish shart emas yoki True qilib saqlash mumkin
            return true;
        }

        // DB dan olingan holatni keshga (masalan, 1 sutka) saqlaymiz
        _cache.Set(cacheKey, command.IsActive, TimeSpan.FromDays(10));

        return command.IsActive;
    }
}
