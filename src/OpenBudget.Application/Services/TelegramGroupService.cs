using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Interfaces;

namespace OpenBudget.Application.Services;

public class TelegramGroupService : ITelegramGroupService
{
    private readonly ITelegramGroupRepository _groupRepository;

    public TelegramGroupService(ITelegramGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<TelegramGroup> TrackGroupActivityAsync(long chatId, string title, string? username = null, CancellationToken cancellationToken = default)
    {
        var existing = await _groupRepository.GetByChatIdAsync(chatId, cancellationToken);
        if (existing == null)
        {
            var newGroup = new TelegramGroup
            {
                ChatId = chatId,
                Title = string.IsNullOrWhiteSpace(title) ? "Guruh" : title,
                Username = username,
                IsActive = true,
                JoinedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };
            await _groupRepository.AddAsync(newGroup, cancellationToken);
            return newGroup;
        }

        existing.Title = string.IsNullOrWhiteSpace(title) ? existing.Title : title;
        existing.Username = username ?? existing.Username;
        existing.IsActive = true;
        existing.LastActiveAt = DateTime.UtcNow;

        await _groupRepository.UpdateAsync(existing, cancellationToken);
        return existing;
    }

    public async Task SetGroupInactiveAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var existing = await _groupRepository.GetByChatIdAsync(chatId, cancellationToken);
        if (existing != null && existing.IsActive)
        {
            existing.IsActive = false;
            await _groupRepository.UpdateAsync(existing, cancellationToken);
        }
    }

    public Task<List<TelegramGroup>> GetActiveGroupsAsync(CancellationToken cancellationToken = default)
    {
        return _groupRepository.GetAllActiveAsync(cancellationToken);
    }
}
