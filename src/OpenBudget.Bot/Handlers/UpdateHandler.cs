using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using OpenBudget.Bot.Services;

namespace OpenBudget.Bot.Handlers;

public class UpdateHandler
{
    private readonly IUserService _userService;
    private readonly GroupMemberHandler _groupMemberHandler;
    private readonly BrokerHandler _brokerHandler;
    private readonly AdminHandler _adminHandler;
    private readonly SuperAdminHandler _superAdminHandler;
    private readonly GuestHandler _guestHandler;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;
    private readonly IDocumentationService _docService;
    private readonly IBotSettingService _botSettingService;
    private readonly ITelegramGroupService _telegramGroupService;

    public UpdateHandler(
        IUserService userService,
        GroupMemberHandler groupMemberHandler,
        BrokerHandler brokerHandler,
        AdminHandler adminHandler,
        SuperAdminHandler superAdminHandler,
        GuestHandler guestHandler,
        INotificationService notificationService,
        IConfiguration configuration,
        IDocumentationService docService,
        IBotSettingService botSettingService,
        ITelegramGroupService telegramGroupService)
    {
        _userService = userService;
        _groupMemberHandler = groupMemberHandler;
        _brokerHandler = brokerHandler;
        _adminHandler = adminHandler;
        _superAdminHandler = superAdminHandler;
        _guestHandler = guestHandler;
        _notificationService = notificationService;
        _configuration = configuration;
        _docService = docService;
        _botSettingService = botSettingService;
        _telegramGroupService = telegramGroupService;
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
                    else if (update.Message?.Chat.Type is ChatType.Group or ChatType.Supergroup)
                    {
                        await HandleGroupMessageAsync(botClient, update.Message, cancellationToken);
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
        var superAdminIds = _configuration.GetSection("TelegramBot:SuperAdminIds").Get<long[]>() ?? Array.Empty<long>();

        if (superAdminIds.Contains(tgUser.Id))
        {
            if (dbUser == null)
            {
                dbUser = await _userService.RegisterUserAsync(tgUser.Id, tgUser.Username, $"{tgUser.FirstName} {tgUser.LastName}".Trim(), UserRole.SuperAdmin, cancellationToken);
                await botClient.SendMessage(message.Chat.Id, "Siz tizimga SuperAdmin sifatida muvaffaqiyatli qo'shildingiz!", cancellationToken: cancellationToken);
            }
            else if (dbUser.Role != UserRole.SuperAdmin)
            {
                await _userService.UpdateRoleAsync(dbUser.Id, UserRole.SuperAdmin, cancellationToken);
                dbUser.Role = UserRole.SuperAdmin;
                await botClient.SendMessage(message.Chat.Id, "Sizning rolingiz SuperAdmin ga o'zgartirildi!", cancellationToken: cancellationToken);
            }
        }

        if (dbUser == null)
        {
            var allowGuests = await _botSettingService.GetAllowGuestRegistrationAsync(cancellationToken);
            if (!allowGuests)
            {
                await botClient.SendMessage(message.Chat.Id, "Tizimga yangi foydalanuvchilar qabul qilinishi vaqtincha to'xtatilgan.", cancellationToken: cancellationToken);
                return;
            }

            dbUser = await _userService.RegisterUserAsync(tgUser.Id, tgUser.Username, $"{tgUser.FirstName} {tgUser.LastName}".Trim(), UserRole.Guest, cancellationToken);
        }

        if (!dbUser.IsActive)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ <b>Sizning hisobingiz bloklangan.</b>\nIltimos, administratorga murojaat qiling.",
                parseMode: ParseMode.Html,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            return;
        }

        if (message.Text == "/start" || message.Text == "/start broker")
        {
            if (message.Text == "/start broker" && dbUser.Role == UserRole.Guest)
            {
                await _userService.UpdateRoleAsync(dbUser.Id, UserRole.Broker, cancellationToken);
                dbUser.Role = UserRole.Broker;
                await botClient.SendMessage(message.Chat.Id, "🎉 <b>Tabriklaymiz!</b> Siz tizimda <b>Broker</b> sifatida ro'yxatdan o'tdingiz.", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            }
            
            await EnsureUserCommandsAsync(botClient, message.Chat.Id, dbUser.Role, cancellationToken);
        }

        switch (dbUser.Role)
        {
            case UserRole.Guest:
                await _guestHandler.HandleMessageAsync(botClient, message, dbUser, cancellationToken);
                break;
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

    private async Task HandleGroupMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.NewChatMembers != null)
        {
            await _groupMemberHandler.HandleNewChatMembersAsync(botClient, message, cancellationToken);
            return;
        }

        if (message.From == null || message.From.IsBot) return;

        // Guruhda yozgan har qanday faol foydalanuvchini agar ro'yxatda bo'lmasa broker sifatida kiritish
        await _groupMemberHandler.RegisterMemberAsync(message.From, message.Chat.Title ?? "Guruh", cancellationToken);

        var text = message.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // /sync buyrug'ini tekshirish (Faqat SuperAdmin uchun)
        if (text.StartsWith("/sync", StringComparison.OrdinalIgnoreCase))
        {
            var dbUser = await _userService.GetByTelegramIdAsync(message.From.Id, cancellationToken);
            var superAdminIds = _configuration.GetSection("TelegramBot:SuperAdminIds").Get<long[]>() ?? Array.Empty<long>();
            bool isSuperAdmin = (dbUser != null && dbUser.Role == UserRole.SuperAdmin) || superAdminIds.Contains(message.From.Id);

            if (!isSuperAdmin)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ <b>/sync buyrug'i faqat SuperAdmin uchun ruxsat etilgan.</b>",
                    parseMode: ParseMode.Html,
                    replyParameters: new ReplyParameters { MessageId = message.MessageId },
                    cancellationToken: cancellationToken);
                return;
            }

            var syncedCount = await _groupMemberHandler.SyncGroupMembersAsync(botClient, message.Chat.Id, message.Chat.Title ?? "Guruh", cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"✅ <b>Guruh muvaffaqiyatli sinxronlandi!</b>\n" +
                      $"👥 Guruh: <b>{message.Chat.Title}</b>\n" +
                      $"🔄 Yangi ro'yxatdan o'tganlar: <b>{syncedCount}</b> ta\n\n" +
                      $"ℹ️ Guruh a'zolari guruhda xabar yozganlarida avtomatik ravishda broker sifatida bazaga kiritiladi.",
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: cancellationToken);
        }
        else if (text.StartsWith("/setcode", StringComparison.OrdinalIgnoreCase))
        {
            var dbUser = await _userService.GetByTelegramIdAsync(message.From.Id, cancellationToken);
            var superAdminIds = _configuration.GetSection("TelegramBot:SuperAdminIds").Get<long[]>() ?? Array.Empty<long>();
            bool isAdmin = dbUser != null && (dbUser.Role == UserRole.SuperAdmin || dbUser.Role == UserRole.Admin) || superAdminIds.Contains(message.From.Id);

            if (!isAdmin)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ <b>/setcode buyrug'i faqat Admin yoki SuperAdmin uchun ruxsat etilgan.</b>",
                    parseMode: ParseMode.Html,
                    replyParameters: new ReplyParameters { MessageId = message.MessageId },
                    cancellationToken: cancellationToken);
                return;
            }

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ <b>Tashabbus kodi kiritilmadi.</b>\nFormat: <code>/setcode [kod_yoki_uuid]</code>",
                    parseMode: ParseMode.Html,
                    replyParameters: new ReplyParameters { MessageId = message.MessageId },
                    cancellationToken: cancellationToken);
                return;
            }

            var code = parts[1];
            var result = await _telegramGroupService.SetInitiativeCodeAsync(message.Chat.Id, code, cancellationToken);
            var emoji = result.Success ? "✅" : "❌";
            
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"{emoji} <b>{result.Message}</b>",
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.From == null) return;

        var dbUser = await _userService.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        var superAdminIds = _configuration.GetSection("TelegramBot:SuperAdminIds").Get<long[]>() ?? Array.Empty<long>();

        if (superAdminIds.Contains(callbackQuery.From.Id))
        {
            if (dbUser == null)
            {
                dbUser = await _userService.RegisterUserAsync(callbackQuery.From.Id, callbackQuery.From.Username, $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}".Trim(), UserRole.SuperAdmin, cancellationToken);
            }
            else if (dbUser.Role != UserRole.SuperAdmin)
            {
                await _userService.UpdateRoleAsync(dbUser.Id, UserRole.SuperAdmin, cancellationToken);
                dbUser.Role = UserRole.SuperAdmin;
            }
        }

        if (callbackQuery.Data != null && callbackQuery.Data.StartsWith("sync_group_"))
        {
            bool isSuperAdmin = (dbUser != null && dbUser.Role == UserRole.SuperAdmin) || superAdminIds.Contains(callbackQuery.From.Id);
            if (!isSuperAdmin)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "❌ Faqat SuperAdmin guruhni sinxronlay oladi!",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            var chatIdStr = callbackQuery.Data.Replace("sync_group_", "");
            if (long.TryParse(chatIdStr, out long targetChatId))
            {
                var chatTitle = callbackQuery.Message?.Chat.Title ?? "Guruh";
                var synced = await _groupMemberHandler.SyncGroupMembersAsync(botClient, targetChatId, chatTitle, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, $"✅ Sinxronlandi! {synced} ta yangi a'zo qo'shildi.", showAlert: true, cancellationToken: cancellationToken);

                await botClient.SendMessage(
                    chatId: targetChatId,
                    text: $"✅ <b>Guruh sinxronizatsiyasi yakunlandi!</b>\n🔄 Yangi qo'shilgan a'zolar: <b>{synced}</b> ta.",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            return;
        }

        if (dbUser == null) return;

        if (callbackQuery.Data != null && callbackQuery.Data.StartsWith("doc_"))
        {
            try
            {
                if (callbackQuery.Data == "doc_main")
                {
                    var (mainText, mainKeyboard) = _docService.GetMainMenu(dbUser.Role);
                    await botClient.EditMessageText(
                        chatId: callbackQuery.Message!.Chat.Id,
                        messageId: callbackQuery.Message.MessageId,
                        text: mainText,
                        parseMode: ParseMode.Html,
                        replyMarkup: mainKeyboard,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    var (secText, secKeyboard) = _docService.GetSectionContent(callbackQuery.Data, dbUser.Role);
                    await botClient.EditMessageText(
                        chatId: callbackQuery.Message!.Chat.Id,
                        messageId: callbackQuery.Message.MessageId,
                        text: secText,
                        parseMode: ParseMode.Html,
                        replyMarkup: secKeyboard,
                        cancellationToken: cancellationToken);
                }
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            }
            catch
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            }
            return;
        }

        if (dbUser.Role == UserRole.Broker)
        {
            await _brokerHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
        }
        else if (dbUser.Role == UserRole.Admin)
        {
            await _adminHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
        }
        else if (dbUser.Role == UserRole.SuperAdmin)
        {
            await _superAdminHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
        }

        try
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch
        {
            // Ignore if already answered
        }
    }

    private async Task EnsureUserCommandsAsync(ITelegramBotClient botClient, long chatId, UserRole role, CancellationToken cancellationToken)
    {
        try
        {
            BotCommand[] commands = role switch
            {
                UserRole.SuperAdmin => new[]
                {
                    new BotCommand { Command = "start", Description = "Asosiy menyu" },
                    new BotCommand { Command = "globalstats", Description = "Global statistika" },
                    new BotCommand { Command = "groups", Description = "Ulangan guruhlar" },
                    new BotCommand { Command = "sync", Description = "Guruh a'zolarini sinxronlash" },
                    new BotCommand { Command = "assignadmin", Description = "Admin tayinlash" },
                    new BotCommand { Command = "brokers", Description = "Brokerlar ro'yxati" },
                    new BotCommand { Command = "confirm", Description = "Ovoz tasdiqlash" },
                    new BotCommand { Command = "vote", Description = "Ovoz qo'shish" },
                    new BotCommand { Command = "myvotes", Description = "Mening ovozlarim" },
                    new BotCommand { Command = "mystats", Description = "Mening statistikam" },
                    new BotCommand { Command = "info", Description = "Loyiha ma'lumotlari" }
                },
                UserRole.Admin => new[]
                {
                    new BotCommand { Command = "start", Description = "Asosiy menyu" },
                    new BotCommand { Command = "brokers", Description = "Brokerlar ro'yxati va boshqarish" },
                    new BotCommand { Command = "confirm", Description = "Ovoz tasdiqlash" },
                    new BotCommand { Command = "adminstats", Description = "Brokerlar statistikasi" },
                    new BotCommand { Command = "vote", Description = "Ovoz qo'shish" },
                    new BotCommand { Command = "myvotes", Description = "Mening ovozlarim" },
                    new BotCommand { Command = "mystats", Description = "Mening statistikam" },
                    new BotCommand { Command = "info", Description = "Loyiha ma'lumotlari" }
                },
                UserRole.Broker => new[]
                {
                    new BotCommand { Command = "start", Description = "Asosiy menyu" },
                    new BotCommand { Command = "vote", Description = "Ovoz qo'shish" },
                    new BotCommand { Command = "myvotes", Description = "Mening ovozlarim" },
                    new BotCommand { Command = "mystats", Description = "Mening statistikam" },
                    new BotCommand { Command = "info", Description = "Loyiha ma'lumotlari" }
                },
                _ => new[] // Guest
                {
                    new BotCommand { Command = "start", Description = "Asosiy menyu" },
                    new BotCommand { Command = "request", Description = "Brokerlik so'rovi yuborish" },
                    new BotCommand { Command = "info", Description = "Loyiha ma'lumotlari" }
                }
            };

            await botClient.SetMyCommands(
                commands: commands,
                scope: new BotCommandScopeChat { ChatId = chatId },
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Fallback gracefully if scope setting fails
        }
    }
}

