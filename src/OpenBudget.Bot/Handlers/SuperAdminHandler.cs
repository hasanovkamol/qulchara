using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.Services;
using OpenBudget.Bot.Helpers;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = OpenBudget.Domain.Entities.User;

namespace OpenBudget.Bot.Handlers;

public class SuperAdminHandler
{
    private readonly IUserService _userService;
    private readonly IVoteService _voteService;
    private readonly ITelegramGroupService _groupService;
    private readonly IConfiguration _configuration;
    private readonly BrokerHandler _brokerHandler;

    public SuperAdminHandler(
        IUserService userService, 
        IVoteService voteService, 
        ITelegramGroupService groupService,
        IConfiguration configuration,
        BrokerHandler brokerHandler)
    {
        _userService = userService;
        _voteService = voteService;
        _groupService = groupService;
        _configuration = configuration;
        _brokerHandler = brokerHandler;
    }

    public static ReplyKeyboardMarkup GetMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("🌍 Global Statistika"), new KeyboardButton("🛡 Admin tayinlash") },
            new[] { new KeyboardButton("👥 Brokerlar ro'yxati"), new KeyboardButton("📢 Ulangan guruhlar") },
            new[] { new KeyboardButton("✅ Ovoz tasdiqlash"), new KeyboardButton("📝 Ovoz qo'shish") },
            new[] { new KeyboardButton("📋 Mening ovozlarim"), new KeyboardButton("📊 Mening statistikam") },
            new[] { new KeyboardButton("ℹ️ Loyiha ma'lumotlari") }
        }) { ResizeKeyboard = true };
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, User dbUser, CancellationToken cancellationToken)
    {
        var text = message.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var replyMarkup = GetMenuKeyboard();

        if (text == "/start")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Salom SuperAdmin! Quyidagi boshqaruv menyusidan foydalaning.",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "🔙 Bekor qilish" || text == "/cancel")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Amal bekor qilindi.",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "🌍 Global Statistika" || text == "/globalstats")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            var stats = await _userService.GetGlobalStatsAsync(cancellationToken);
            var statsText = $"🌍 <b>Global Statistika</b>\n" +
                            $"━━━━━━━━━━━━━━━━━━\n" +
                            $"👥 Brokerlar soni: <b>{stats.TotalBrokers}</b>\n" +
                            $"🛡 Adminlar soni: <b>{stats.TotalAdmins}</b>\n" +
                            $"📋 Jami ovozlar: <b>{stats.TotalVotes}</b>\n" +
                            $"✅ Tasdiqlangan: <b>{stats.ConfirmedVotes}</b>\n" +
                            $"⏳ Kutilmoqda: <b>{stats.PendingVotes}</b>\n" +
                            $"❌ Rad etilgan: <b>{stats.RejectedVotes}</b>\n" +
                            $"━━━━━━━━━━━━━━━━━━";
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: statsText,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "🛡 Admin tayinlash" || text == "/assignadmin")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForAdminId, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Yangi adminga aylantirmoqchi bo'lgan foydalanuvchining ID raqamini kiriting:",
                replyMarkup: BotConstants.GetCancelKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "👥 Brokerlar ro'yxati" || text == "/brokers")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendBrokersPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text == "📢 Ulangan guruhlar" || text == "/groups")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendGroupsListAsync(botClient, message.Chat.Id, cancellationToken);
            return;
        }

        if (text == "✅ Ovoz tasdiqlash" || text == "/confirm")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForConfirmation, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Tasdiqlash uchun oxirgi 3 xona va vaqtni kiriting.\nFormat: <code>123 15:30</code>",
                parseMode: ParseMode.Html,
                replyMarkup: BotConstants.GetCancelKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "📝 Ovoz qo'shish" || text == "/vote")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForVote, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Ovoz berish uchun 9 xonali telefon raqamni kiriting.\n+998 avtomatik qo'shiladi.",
                replyMarkup: BotConstants.GetCancelKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "📋 Mening ovozlarim" || text == "/myvotes")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await _brokerHandler.SendMyVotesAsync(botClient, message.Chat.Id, dbUser.Id, 1, cancellationToken);
            return;
        }

        if (text == "📊 Mening statistikam" || text == "/mystats")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            var stats = await _voteService.GetBrokerStatsAsync(dbUser.Id, cancellationToken);
            var statsText = $"📊 <b>Sizning statistikangiz</b>\n" +
                            $"━━━━━━━━━━━━━━━━━━\n" +
                            $"📋 Jami ovozlar: <b>{stats.TotalVotes}</b>\n" +
                            $"✅ Tasdiqlangan: <b>{stats.ConfirmedVotes}</b>\n" +
                            $"⏳ Kutilmoqda: <b>{stats.PendingVotes}</b>\n" +
                            $"❌ Rad etilgan: <b>{stats.RejectedVotes}</b>\n" +
                            $"━━━━━━━━━━━━━━━━━━";
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: statsText,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "ℹ️ Loyiha ma'lumotlari" || text == "/info")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: BotConstants.ProjectInfoHtml,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text.StartsWith("/")) return;

        if (dbUser.BotState == BotState.WaitingForAdminId)
        {
            if (int.TryParse(text, out int targetUserId))
            {
                var result = await _userService.AssignRoleAsync(targetUserId, UserRole.Admin, UserRole.SuperAdmin, cancellationToken);
                var reply = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
                await botClient.SendMessage(message.Chat.Id, reply, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(message.Chat.Id, "Noto'g'ri ID kiritildi.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            return;
        }

        if (dbUser.BotState == BotState.WaitingForConfirmation)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Noto'g'ri format. Format: <code>123 15:30</code>",
                    parseMode: ParseMode.Html,
                    replyMarkup: BotConstants.GetCancelKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var last3 = parts[0];
            if (last3.Length != 3 || !int.TryParse(last3, out _))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Oxirgi 3 ta raqam noto'g'ri. Misol: 123",
                    replyMarkup: BotConstants.GetCancelKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            if (!TimeSpan.TryParse(parts[1], out TimeSpan time))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Vaqt formati noto'g'ri. Misol: 15:30",
                    replyMarkup: BotConstants.GetCancelKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var localTimeNow = OpenBudget.Application.Helpers.DateTimeHelper.UzbekistanNow;
            var targetLocalTime = localTimeNow.Date + time;
            
            if (targetLocalTime > localTimeNow.AddHours(1)) 
            {
                targetLocalTime = targetLocalTime.AddDays(-1);
            }

            var windowHours = int.Parse(_configuration["VoteSettings:ConfirmTimeWindowHours"] ?? "1");

            var result = await _voteService.ConfirmVoteAsync(dbUser.Id, last3, targetLocalTime, TimeSpan.FromHours(windowHours), cancellationToken);

            var reply = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, reply, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return;
        }

        if (dbUser.BotState == BotState.WaitingForVote)
        {
            var result = await _voteService.AddVoteAsync(dbUser.Id, text, cancellationToken);
            var replyText = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, replyText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(message.Chat.Id, "Iltimos, quyidagi menyu tugmalaridan foydalaning.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, User dbUser, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        if (string.IsNullOrEmpty(data)) return;

        if (data.StartsWith("toggle_block_"))
        {
            var parts = data.Split('_');
            if (parts.Length >= 4 && int.TryParse(parts[2], out int targetUserId) && int.TryParse(parts[3], out int page))
            {
                var result = await _userService.ToggleUserBlockAsync(targetUserId, dbUser.Role, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, result.Message, showAlert: true, cancellationToken: cancellationToken);
                await EditBrokersPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
                return;
            }
        }
        else if (data.StartsWith("bpage_"))
        {
            if (int.TryParse(data.Replace("bpage_", ""), out int page))
            {
                await EditBrokersPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
        }
        else if (data.StartsWith("page_"))
        {
            await _brokerHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
        }
    }

    private async Task SendGroupsListAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var groups = await _groupService.GetActiveGroupsAsync(cancellationToken);
        if (!groups.Any())
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "📢 <b>Bot hozircha hech qaysi guruhga qo'shilmagan.</b>\n\nBotni kerakli guruhga qo'shsangiz, u avtomatik ravishda ushbu ro'yxatda paydo bo'ladi.",
                parseMode: ParseMode.Html,
                replyMarkup: GetMenuKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        var text = $"📢 <b>Ulangan Guruhlar ro'yxati</b> ({groups.Count} ta)\n" +
                   $"━━━━━━━━━━━━━━━━━━\n";

        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            var usernameText = string.IsNullOrEmpty(g.Username) ? "" : $" (@{g.Username})";
            text += $"{i + 1}. <b>{g.Title}</b>{usernameText}\n" +
                    $"   🆔 Chat ID: <code>{g.ChatId}</code>\n" +
                    $"   📅 Qo'shilgan: {g.JoinedAt:dd.MM.yyyy HH:mm}\n\n";

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🔄 {g.Title} ni sinxronlash", $"sync_group_{g.ChatId}")
            });
        }

        text += $"━━━━━━━━━━━━━━━━━━\n" +
                $"💡 <i>Guruh ma'murlarini tizimga ro'yxatga olish uchun yuqoridagi 'Sinxronlash' tugmasini bosing.</i>";

        var markup = new InlineKeyboardMarkup(buttons);

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }

    public async Task SendBrokersPageAsync(ITelegramBotClient botClient, long chatId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateBrokersPageAsync(page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    public async Task EditBrokersPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        try
        {
            var (text, markup) = await GenerateBrokersPageAsync(page, cancellationToken);
            await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
        }
        catch
        {
            // Fallback for message not modified or editing exceptions
        }
    }

    public async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateBrokersPageAsync(int page, CancellationToken cancellationToken)
    {
        var allUsers = await _userService.GetAllUsersAsync(cancellationToken);
        var brokers = allUsers.Where(u => u.Role == UserRole.Broker).OrderByDescending(u => u.CreatedAt).ToList();

        if (!brokers.Any())
        {
            return ("👥 <b>Brokerlar topilmadi.</b>", null);
        }

        int pageSize = 5;
        var totalPages = (int)Math.Ceiling((double)brokers.Count / pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pagedBrokers = brokers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var text = $"👥 <b>Brokerlar Ro'yxati</b> (Sahifa {page}/{totalPages}, Jami: {brokers.Count} ta)\n" +
                   $"━━━━━━━━━━━━━━━━━━\n";

        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < pagedBrokers.Count; i++)
        {
            var b = pagedBrokers[i];
            var index = (page - 1) * pageSize + i + 1;
            var usernameText = string.IsNullOrEmpty(b.Username) ? "" : $" (@{b.Username})";
            var statusBadge = b.IsActive ? "🟢 <b>Faol</b>" : "🔴 <b>Bloklangan</b>";

            text += $"{index}. <b>{b.FullName ?? "Noma'lum"}</b>{usernameText}\n" +
                    $"   🆔 ID: <code>{b.Id}</code> | 📞 TG ID: <code>{b.TelegramId}</code>\n" +
                    $"   ⚡️ Holati: {statusBadge}\n\n";

            var shortName = b.FullName ?? b.Username ?? b.Id.ToString();
            if (shortName.Length > 15) shortName = shortName.Substring(0, 12) + "...";

            var blockBtnText = b.IsActive 
                ? $"🔴 Bloklash ({shortName})" 
                : $"🟢 Blokdan chiqarish ({shortName})";

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(blockBtnText, $"toggle_block_{b.Id}_{page}")
            });
        }

        text += $"━━━━━━━━━━━━━━━━━━";

        var prevPage = page > 1 ? page - 1 : 1;
        var nextPage = page < totalPages ? page + 1 : totalPages;

        var prevButton = page > 1 ? InlineKeyboardButton.WithCallbackData("◀️ Oldingi", $"bpage_{prevPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");
        var currButton = InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "noop");
        var nextButton = page < totalPages ? InlineKeyboardButton.WithCallbackData("Keyingi ▶️", $"bpage_{nextPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");

        buttons.Add(new[] { prevButton, currButton, nextButton });

        var markup = new InlineKeyboardMarkup(buttons);
        return (text, markup);
    }
}


