using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = OpenBudget.Domain.Entities.User; // Alias to avoid collision

namespace OpenBudget.Bot.Handlers;

public class BrokerHandler
{
    private readonly IVoteService _voteService;
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;

    public BrokerHandler(IVoteService voteService, IConfiguration configuration, IUserService userService)
    {
        _voteService = voteService;
        _configuration = configuration;
        _userService = userService;
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, User dbUser, CancellationToken cancellationToken)
    {
        var text = message.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var webAppUrl = _configuration["MiniApp:Url"] ?? "";
        var replyMarkup = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📝 Ovoz qo'shish"), new KeyboardButton("📋 Mening ovozlarim") },
            new[] { new KeyboardButton("📊 Statistikam"), new KeyboardButton("📱 Mini App") { WebApp = new WebAppInfo { Url = webAppUrl } } }
        }) { ResizeKeyboard = true };

        if (text == "/start")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Salom Broker! Ovoz kiritish va statistikani ko'rish uchun quyidagi tugmalardan foydalaning.",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "📝 Ovoz qo'shish")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.WaitingForVote, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, "Ovoz berish uchun 9 xonali telefon raqamni kiriting.\n+998 avtomatik qo'shiladi.", cancellationToken: cancellationToken);
            return;
        }

        if (text == "📋 Mening ovozlarim" || text == "/myvotes")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await SendMyVotesAsync(botClient, message.Chat.Id, dbUser.Id, 1, cancellationToken);
            return;
        }

        if (text == "📊 Statistikam" || text == "/mystats")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            var stats = await _voteService.GetBrokerStatsAsync(dbUser.Id, cancellationToken);
            var statsText = $"📊 Sizning statistikangiz\n" +
                            $"━━━━━━━━━━━━━━━━━━\n" +
                            $"📋 Jami ovozlar: {stats.TotalVotes}\n" +
                            $"✅ Tasdiqlangan: {stats.ConfirmedVotes}\n" +
                            $"⏳ Kutilmoqda: {stats.PendingVotes}\n" +
                            $"❌ Rad etilgan: {stats.RejectedVotes}\n" +
                            $"━━━━━━━━━━━━━━━━━━";
            await botClient.SendMessage(message.Chat.Id, statsText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return;
        }

        if (text.StartsWith("/")) return; // unknown command

        if (dbUser.BotState == Domain.Enums.BotState.WaitingForVote)
        {
            // Attempt to add vote
            var result = await _voteService.AddVoteAsync(dbUser.Id, text, cancellationToken);
            var replyText = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            
            // Go back to default state
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, replyText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(message.Chat.Id, "Iltimos, tugmalardan foydalaning.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
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

    private async Task SendMyVotesAsync(ITelegramBotClient botClient, long chatId, int brokerId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateVotesPageAsync(brokerId, page, cancellationToken);
        await botClient.SendMessage(chatId, text, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    private async Task EditMyVotesAsync(ITelegramBotClient botClient, long chatId, int messageId, int brokerId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateVotesPageAsync(brokerId, page, cancellationToken);
        await botClient.EditMessageText(chatId, messageId, text, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    private async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateVotesPageAsync(int brokerId, int page, CancellationToken cancellationToken)
    {
        var pageSize = int.Parse(_configuration["VoteSettings:PageSize"] ?? "1");
        var pagedResult = await _voteService.GetBrokerVotesPagedAsync(brokerId, page, pageSize, cancellationToken);

        if (!pagedResult.Items.Any())
        {
            return ("Sizda hali ovozlar yo'q.", null);
        }

        var vote = pagedResult.Items.First(); // since pageSize=1
        var statusEmoji = vote.Status == Domain.Enums.VoteStatus.Pending ? "⏳ Kutilmoqda" :
                          (vote.Status == Domain.Enums.VoteStatus.Confirmed ? "✅ Tasdiqlangan" : "❌ Rad etilgan");

        var text = $"📋 Ovoz {page}/{Math.Max(1, (int)Math.Ceiling((double)pagedResult.TotalCount / pageSize))}\n" +
                   $"━━━━━━━━━━━━━━━━\n" +
                   $"📱 {vote.PhoneNumber}\n" +
                   $"🕐 {vote.VotedAt.ToLocalTime():HH:mm (dd.MM.yyyy)}\n" +
                   $"{statusEmoji}\n" +
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
