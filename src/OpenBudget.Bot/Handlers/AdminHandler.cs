using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = OpenBudget.Domain.Entities.User; // Alias to avoid collision

namespace OpenBudget.Bot.Handlers;

public class AdminHandler
{
    private readonly IVoteService _voteService;
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;

    public AdminHandler(IVoteService voteService, IConfiguration configuration, IUserService userService)
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
            new[] { new KeyboardButton("✅ Ovoz tasdiqlash"), new KeyboardButton("📊 Brokerlar statistikasi") },
            new[] { new KeyboardButton("📉 Tasdiqlanmagan ovozlar"), new KeyboardButton("📱 Mini App") { WebApp = new WebAppInfo { Url = webAppUrl } } }
        }) { ResizeKeyboard = true };

        if (text == "/start")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Salom Admin! Boshqaruv tugmalaridan foydalaning.",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "✅ Ovoz tasdiqlash")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.WaitingForConfirmation, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, "Tasdiqlash uchun oxirgi 3 xona va vaqtni kiriting.\nFormat: 123 15:30", cancellationToken: cancellationToken);
            return;
        }

        if (text == "📊 Brokerlar statistikasi")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            var stats = await _userService.GetGlobalStatsAsync(cancellationToken);
            var statsText = $"📊 Global Statistika\n" +
                            $"━━━━━━━━━━━━━━━━━━\n" +
                            $"👥 Brokerlar soni: {stats.TotalBrokers}\n" +
                            $"📋 Jami ovozlar: {stats.TotalVotes}\n" +
                            $"✅ Tasdiqlangan: {stats.ConfirmedVotes}\n" +
                            $"⏳ Kutilmoqda: {stats.PendingVotes}\n" +
                            $"❌ Rad etilgan: {stats.RejectedVotes}\n" +
                            $"━━━━━━━━━━━━━━━━━━\nTo'liq hisobot uchun Mini App ga kiring.";
            await botClient.SendMessage(message.Chat.Id, statsText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return;
        }

        if (text == "📉 Tasdiqlanmagan ovozlar")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, "Tasdiqlanmagan ovozlar ro'yxatini ko'rish uchun Mini App'dan foydalaning.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return;
        }

        if (text.StartsWith("/")) return;

        if (dbUser.BotState == Domain.Enums.BotState.WaitingForConfirmation)
        {
            // Expected format: "123 15:30" or "123 15:30:45"
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await botClient.SendMessage(message.Chat.Id, "Noto'g'ri format. Format: 123 15:30", cancellationToken: cancellationToken);
                return;
            }

            var last3 = parts[0];
            if (last3.Length != 3 || !int.TryParse(last3, out _))
            {
                await botClient.SendMessage(message.Chat.Id, "Oxirgi 3 ta raqam noto'g'ri.", cancellationToken: cancellationToken);
                return;
            }

            if (!TimeSpan.TryParse(parts[1], out TimeSpan time))
            {
                await botClient.SendMessage(message.Chat.Id, "Vaqt formati noto'g'ri. Misol: 15:30", cancellationToken: cancellationToken);
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
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, reply, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(message.Chat.Id, "Iltimos, tugmalardan foydalaning.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
    }
}
