using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OpenBudget.Bot.Handlers;

public class GroupMemberHandler
{
    private readonly IUserService _userService;
    private readonly ITelegramGroupService _groupService;
    private readonly INotificationService _notificationService;

    public GroupMemberHandler(
        IUserService userService, 
        ITelegramGroupService groupService,
        INotificationService notificationService)
    {
        _userService = userService;
        _groupService = groupService;
        _notificationService = notificationService;
    }

    public async Task HandleChatMemberUpdatedAsync(ITelegramBotClient botClient, ChatMemberUpdated chatMemberUpdated, CancellationToken cancellationToken)
    {
        var me = await botClient.GetMe(cancellationToken);
        if (chatMemberUpdated.NewChatMember.User.Id == me.Id)
        {
            if (chatMemberUpdated.NewChatMember.Status is ChatMemberStatus.Member or ChatMemberStatus.Administrator)
            {
                await _groupService.TrackGroupActivityAsync(chatMemberUpdated.Chat.Id, chatMemberUpdated.Chat.Title ?? "Guruh", chatMemberUpdated.Chat.Username, cancellationToken);
                await SendGroupWelcomeAsync(botClient, chatMemberUpdated.Chat.Id, chatMemberUpdated.Chat.Title ?? "Guruh", cancellationToken);
            }
            else if (chatMemberUpdated.NewChatMember.Status is ChatMemberStatus.Left or ChatMemberStatus.Kicked)
            {
                await _groupService.SetGroupInactiveAsync(chatMemberUpdated.Chat.Id, cancellationToken);
            }
            return;
        }

        var newMember = chatMemberUpdated.NewChatMember;
        if (newMember.Status is ChatMemberStatus.Member or ChatMemberStatus.Administrator or ChatMemberStatus.Creator)
        {
            await _groupService.TrackGroupActivityAsync(chatMemberUpdated.Chat.Id, chatMemberUpdated.Chat.Title ?? "Guruh", chatMemberUpdated.Chat.Username, cancellationToken);
            await RegisterMemberAsync(newMember.User, chatMemberUpdated.Chat.Title ?? "Guruh", cancellationToken);
        }
    }

    public async Task HandleNewChatMembersAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.NewChatMembers == null) return;

        await _groupService.TrackGroupActivityAsync(message.Chat.Id, message.Chat.Title ?? "Guruh", message.Chat.Username, cancellationToken);

        var me = await botClient.GetMe(cancellationToken);
        bool botAdded = false;

        foreach (var member in message.NewChatMembers)
        {
            if (member.Id == me.Id)
            {
                botAdded = true;
            }
            else
            {
                await RegisterMemberAsync(member, message.Chat.Title ?? "Guruh", cancellationToken);
            }
        }

        if (botAdded)
        {
            await SendGroupWelcomeAsync(botClient, message.Chat.Id, message.Chat.Title ?? "Guruh", cancellationToken);
        }
    }

    public async Task SendGroupWelcomeAsync(ITelegramBotClient botClient, long chatId, string chatTitle, CancellationToken cancellationToken)
    {
        var syncMarkup = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Guruhni sinxronlash (SuperAdmin)", $"sync_group_{chatId}")
        });

        var text = $"👋 <b>OpenBudget Bot {chatTitle} guruhiga muvaffaqiyatli qo'shildi!</b>\n\n" +
                   "🛡 <b>SuperAdminlar uchun:</b> Guruh a'zolari va ma'murlarini bazaga sinxronlash uchun /sync buyrug'idan foydalaning yoki pastdagi tugmani bosing.\n" +
                   "👥 Guruh a'zolari xabar yozishlari bilan avtomatik ravishda broker sifatida ro'yxatga olinadi.";

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: syncMarkup,
            cancellationToken: cancellationToken);
    }

    public async Task<int> SyncGroupMembersAsync(ITelegramBotClient botClient, long chatId, string chatTitle, CancellationToken cancellationToken)
    {
        int synced = 0;
        try
        {
            await _groupService.TrackGroupActivityAsync(chatId, chatTitle, null, cancellationToken);
            var admins = await botClient.GetChatAdministrators(chatId, cancellationToken: cancellationToken);
            foreach (var admin in admins)
            {
                if (admin.User.IsBot) continue;

                var existing = await _userService.GetByTelegramIdAsync(admin.User.Id, cancellationToken);
                if (existing == null)
                {
                    await _userService.RegisterUserAsync(admin.User.Id, admin.User.Username, $"{admin.User.FirstName} {admin.User.LastName}".Trim(), UserRole.Broker, cancellationToken);
                    synced++;
                }
            }

            await _notificationService.NotifyInfoAsync($"Guruh sinxronizatsiyasi bajarildi:\nGuruh: {chatTitle} (ID: {chatId})\nYangi qo'shilganlar: {synced} ta", cancellationToken);
        }
        catch (Exception ex)
        {
            await _notificationService.NotifyErrorAsync(ex, chatId, "SyncGroupMembersAsync", cancellationToken);
        }

        return synced;
    }

    public async Task RegisterMemberAsync(Telegram.Bot.Types.User tgUser, string groupName, CancellationToken cancellationToken)
    {
        if (tgUser.IsBot) return;

        try
        {
            await _userService.RegisterUserAsync(tgUser.Id, tgUser.Username, $"{tgUser.FirstName} {tgUser.LastName}".Trim(), UserRole.Broker, cancellationToken);
        }
        catch (Exception ex)
        {
            await _notificationService.NotifyErrorAsync(ex, tgUser.Id, "RegisterMemberAsync", cancellationToken);
        }
    }
}


