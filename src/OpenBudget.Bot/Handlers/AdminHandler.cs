using System;
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
using User = OpenBudget.Domain.Entities.User; // Alias to avoid collision

namespace OpenBudget.Bot.Handlers;

public class AdminHandler
{
    private readonly IVoteService _voteService;
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly BrokerHandler _brokerHandler;
    private readonly SuperAdminHandler _superAdminHandler;

    public AdminHandler(
        IVoteService voteService, 
        IConfiguration configuration, 
        IUserService userService, 
        BrokerHandler brokerHandler,
        SuperAdminHandler superAdminHandler)
    {
        _voteService = voteService;
        _configuration = configuration;
        _userService = userService;
        _brokerHandler = brokerHandler;
        _superAdminHandler = superAdminHandler;
    }

    public static ReplyKeyboardMarkup GetMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("👥 Brokerlar ro'yxati"), new KeyboardButton("📊 Brokerlar statistikasi") },
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
                text: "Salom Admin! Boshqaruv menyusidan foydalaning.",
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

        if (text == "👥 Brokerlar ro'yxati" || text == "/brokers")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await _superAdminHandler.SendBrokersPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
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

        if (text == "📊 Brokerlar statistikasi" || text == "/adminstats")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            var stats = await _userService.GetGlobalStatsAsync(cancellationToken);
            var statsText = $"📊 <b>Brokerlar va Tizim statistikasi</b>\n" +
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

            var localTimeNow = DateTime.UtcNow.AddHours(5);
            var targetLocalTime = localTimeNow.Date + time;
            
            if (targetLocalTime > localTimeNow.AddHours(1)) 
            {
                targetLocalTime = targetLocalTime.AddDays(-1);
            }

            var targetUtcTime = targetLocalTime.AddHours(-5);
            var windowHours = int.Parse(_configuration["VoteSettings:ConfirmTimeWindowHours"] ?? "1");

            var result = await _voteService.ConfirmVoteAsync(dbUser.Id, last3, targetUtcTime, TimeSpan.FromHours(windowHours), cancellationToken);

            var reply = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, reply, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
        else if (dbUser.BotState == BotState.WaitingForVote)
        {
            var result = await _voteService.AddVoteAsync(dbUser.Id, text, cancellationToken);
            var replyText = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, replyText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(message.Chat.Id, "Iltimos, quyidagi menyu tugmalaridan foydalaning.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, User dbUser, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        if (!string.IsNullOrEmpty(data) && (data.StartsWith("bpage_") || data.StartsWith("toggle_block_")))
        {
            await _superAdminHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
            return;
        }

        await _brokerHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
    }
}

