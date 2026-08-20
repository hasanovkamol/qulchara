using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.Helpers;
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
                JoinedAt = DateTimeHelper.UzbekistanNow,
                LastActiveAt = DateTimeHelper.UzbekistanNow
            };
            await _groupRepository.AddAsync(newGroup, cancellationToken);
            return newGroup;
        }

        existing.Title = string.IsNullOrWhiteSpace(title) ? existing.Title : title;
        existing.Username = username ?? existing.Username;
        existing.IsActive = true;
        existing.LastActiveAt = DateTimeHelper.UzbekistanNow;

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

    public async Task<List<TelegramGroup>> GetActiveGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _groupRepository.GetAllActiveAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message)> SetInitiativeCodeAsync(long chatId, string initiativeCode, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByChatIdAsync(chatId, cancellationToken);
        if (group == null)
        {
            return (false, "Guruh topilmadi.");
        }
        
        group.InitiativeCode = initiativeCode.Trim();
        await _groupRepository.UpdateAsync(group, cancellationToken);
        
        return (true, "Tashabbus kodi muvaffaqiyatli saqlandi.");
    }

    public async Task<List<TelegramGroup>> GetActiveGroupsWithCodeAsync(CancellationToken cancellationToken = default)
    {
        var allActive = await _groupRepository.GetAllActiveAsync(cancellationToken);
        return allActive.Where(g => !string.IsNullOrEmpty(g.InitiativeCode)).ToList();
    }
}
