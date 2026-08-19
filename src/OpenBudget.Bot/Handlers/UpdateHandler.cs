using System;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OpenBudget.Bot.Handlers;

public class UpdateHandler
{
    private readonly IUserService _userService;
    private readonly GroupMemberHandler _groupMemberHandler;
    private readonly BrokerHandler _brokerHandler;
    private readonly AdminHandler _adminHandler;
    private readonly SuperAdminHandler _superAdminHandler;
    private readonly INotificationService _notificationService;

    public UpdateHandler(
        IUserService userService,
        GroupMemberHandler groupMemberHandler,
        BrokerHandler brokerHandler,
        AdminHandler adminHandler,
        SuperAdminHandler superAdminHandler,
        INotificationService notificationService)
    {
        _userService = userService;
        _groupMemberHandler = groupMemberHandler;
        _brokerHandler = brokerHandler;
        _adminHandler = adminHandler;
        _superAdminHandler = superAdminHandler;
        _notificationService = notificationService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message?.Chat.Type == ChatType.Private)
                    {
                        await HandlePrivateMessageAsync(botClient, update.Message, cancellationToken);
                    }
                    else if (update.Message?.NewChatMembers != null)
                    {
                        await _groupMemberHandler.HandleNewChatMembersAsync(botClient, update.Message, cancellationToken);
                    }
                    break;
                case UpdateType.CallbackQuery:
                    await HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken);
                    break;
                case UpdateType.ChatMember:
                    await _groupMemberHandler.HandleChatMemberUpdatedAsync(botClient, update.ChatMember!, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            await _notificationService.NotifyErrorAsync(ex, update.Message?.From?.Id, "UpdateHandler", cancellationToken);
        }
    }

    private async Task HandlePrivateMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.From == null) return;

        var tgUser = message.From;
        var dbUser = await _userService.GetByTelegramIdAsync(tgUser.Id, cancellationToken);
        
        if (dbUser == null)
        {
            // Optional: Avto register qilishni xohlasak, shuni ishlatamiz. 
            // Lekin biz faqat guruh orqali ro'yxatdan o'tganlarni qabul qilamiz deganmiz.
            // Shuning uchun:
            await botClient.SendMessage(message.Chat.Id, "Siz hali ro'yxatdan o'tmagansiz. Guruhga qo'shiling.", cancellationToken: cancellationToken);
            return;
        }

        switch (dbUser.Role)
        {
            case UserRole.Broker:
                await _brokerHandler.HandleMessageAsync(botClient, message, dbUser, cancellationToken);
                break;
            case UserRole.Admin:
                await _adminHandler.HandleMessageAsync(botClient, message, dbUser, cancellationToken);
                break;
            case UserRole.SuperAdmin:
                await _superAdminHandler.HandleMessageAsync(botClient, message, dbUser, cancellationToken);
                break;
        }
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.From == null) return;

        var dbUser = await _userService.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (dbUser == null) return;

        if (dbUser.Role == UserRole.Broker)
        {
            await _brokerHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
        }

        // Answer callback to remove loading state
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
    }
}
