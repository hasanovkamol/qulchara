using System;
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
using User = OpenBudget.Domain.Entities.User; // Alias to avoid collision

using OpenBudget.Bot.Services;

namespace OpenBudget.Bot.Handlers;

public class BrokerHandler
{
    private readonly IVoteService _voteService;
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly IDocumentationService _docService;

    public BrokerHandler(IVoteService voteService, IConfiguration configuration, IUserService userService, IDocumentationService docService)
    {
        _voteService = voteService;
        _configuration = configuration;
        _userService = userService;
        _docService = docService;
    }

    public static ReplyKeyboardMarkup GetMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📝 Ovoz qo'shish"), new KeyboardButton("📋 Mening ovozlarim") },
            new[] { new KeyboardButton("📊 Statistikam"), new KeyboardButton("ℹ️ Loyiha ma'lumotlari") }
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
                text: "Salom Broker! Ovoz kiritish va statistikani ko'rish uchun quyidagi menyudan foydalaning.",
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
            await SendMyVotesAsync(botClient, message.Chat.Id, dbUser.Id, 1, cancellationToken);
            return;
        }

        if (text == "📊 Statistikam" || text == "/mystats")
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
            var (docText, docKeyboard) = _docService.GetMainMenu(dbUser.Role);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: docText,
                parseMode: ParseMode.Html,
                replyMarkup: docKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (text.StartsWith("/")) return; // unknown command

        if (dbUser.BotState == BotState.WaitingForVote)
        {
            var result = await _voteService.AddVoteAsync(dbUser.Id, text, cancellationToken);
            var replyText = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: replyText,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Iltimos, quyidagi menyu tugmalaridan foydalaning.",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, User dbUser, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        if (string.IsNullOrEmpty(data)) return;

        if (data.StartsWith("page_"))
        {
            if (int.TryParse(data.Replace("page_", ""), out int page))
            {
                await EditMyVotesAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, dbUser.Id, page, cancellationToken);
            }
        }
    }

    public async Task SendMyVotesAsync(ITelegramBotClient botClient, long chatId, int brokerId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateVotesPageAsync(brokerId, page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    public async Task EditMyVotesAsync(ITelegramBotClient botClient, long chatId, int messageId, int brokerId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateVotesPageAsync(brokerId, page, cancellationToken);
        await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    private async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateVotesPageAsync(int brokerId, int page, CancellationToken cancellationToken)
    {
        var pageSize = int.Parse(_configuration["VoteSettings:PageSize"] ?? "1");
        var pagedResult = await _voteService.GetBrokerVotesPagedAsync(brokerId, page, pageSize, cancellationToken);

        if (!pagedResult.Items.Any())
        {
            return ("Sizda hali kiritilgan ovozlar mavjud emas.", null);
        }

        var vote = pagedResult.Items.First(); // since pageSize=1
        var statusEmoji = vote.Status == VoteStatus.Pending ? "⏳ Kutilmoqda" :
                          (vote.Status == VoteStatus.Confirmed ? "✅ Tasdiqlangan" : "❌ Rad etilgan");

        var text = $"📋 <b>Mening ovozlarim</b> ({page}/{Math.Max(1, (int)Math.Ceiling((double)pagedResult.TotalCount / pageSize))})\n" +
                   $"━━━━━━━━━━━━━━━━\n" +
                   $"📱 Raqam: <code>{vote.PhoneNumber}</code>\n" +
                   $"🕐 Vaqt: {vote.VotedAt:HH:mm (dd.MM.yyyy)}\n" +
                   $"Holat: <b>{statusEmoji}</b>\n" +
                   $"━━━━━━━━━━━━━━━━";

        var prevPage = page > 1 ? page - 1 : 1;
        var totalPages = (int)Math.Ceiling((double)pagedResult.TotalCount / pageSize);
        var nextPage = page < totalPages ? page + 1 : totalPages;

        var prevButton = page > 1 ? InlineKeyboardButton.WithCallbackData("◀️ Oldingi", $"page_{prevPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");
        var currButton = InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "noop");
        var nextButton = page < totalPages ? InlineKeyboardButton.WithCallbackData("Keyingi ▶️", $"page_{nextPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");

        var markup = new InlineKeyboardMarkup(new[]
        {
            new[] { prevButton, currButton, nextButton }
        });

        return (text, markup);
    }
}

