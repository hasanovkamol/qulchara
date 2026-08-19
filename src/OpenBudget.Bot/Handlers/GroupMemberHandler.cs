using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace OpenBudget.Bot.Handlers;

public class GroupMemberHandler
{
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;

    public GroupMemberHandler(IUserService userService, INotificationService notificationService)
    {
        _userService = userService;
        _notificationService = notificationService;
    }

    public async Task HandleChatMemberUpdatedAsync(ITelegramBotClient botClient, ChatMemberUpdated chatMemberUpdated, CancellationToken cancellationToken)
    {
        var newMember = chatMemberUpdated.NewChatMember;
        if (newMember.Status == Telegram.Bot.Types.Enums.ChatMemberStatus.Member ||
            newMember.Status == Telegram.Bot.Types.Enums.ChatMemberStatus.Administrator ||
            newMember.Status == Telegram.Bot.Types.Enums.ChatMemberStatus.Creator)
        {
            await RegisterMemberAsync(newMember.User, chatMemberUpdated.Chat.Title ?? "Group", cancellationToken);
        }
    }

    public async Task HandleNewChatMembersAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.NewChatMembers == null) return;

        foreach (var member in message.NewChatMembers)
        {
            await RegisterMemberAsync(member, message.Chat.Title ?? "Group", cancellationToken);
        }
    }

    private async Task RegisterMemberAsync(Telegram.Bot.Types.User tgUser, string groupName, CancellationToken cancellationToken)
    {
        if (tgUser.IsBot) return;

        try
        {
            var user = await _userService.RegisterUserAsync(tgUser.Id, tgUser.Username, $"{tgUser.FirstName} {tgUser.LastName}".Trim(), UserRole.Broker, cancellationToken);
            // Info log
            await _notificationService.NotifyInfoAsync($"Yangi broker qo'shildi:\nUser: @{tgUser.Username} (ID: {tgUser.Id})\nGroup: {groupName}", cancellationToken);
        }
        catch (Exception ex)
        {
            await _notificationService.NotifyErrorAsync(ex, tgUser.Id, "RegisterMemberAsync", cancellationToken);
        }
    }
}
