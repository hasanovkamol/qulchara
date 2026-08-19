using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = OpenBudget.Domain.Entities.User; // Alias to avoid collision

namespace OpenBudget.Bot.Handlers;

public class AdminHandler
{
    private readonly IVoteService _voteService;
    private readonly IConfiguration _configuration;

    public AdminHandler(IVoteService voteService, IConfiguration configuration)
    {
        _voteService = voteService;
        _configuration = configuration;
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, User dbUser, CancellationToken cancellationToken)
    {
        var text = message.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (text == "/start")
        {
            var webAppUrl = _configuration["MiniApp:Url"];
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Salom Admin! Tasdiqlash uchun oxirgi 3 xona va vaqtni kiriting.\nFormat: 123 15:30",
                cancellationToken: cancellationToken);
            return;
        }

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

        // Combine current UTC date with the specified time (Assuming input is in local time, Uzb is UTC+5)
        // For simplicity, let's assume they enter local time, and we convert to UTC for comparison.
        var localTimeNow = DateTime.UtcNow.AddHours(5);
        var targetLocalTime = localTimeNow.Date + time;
        
        // If they enter a time slightly larger than current time, it might be yesterday's vote
        if (targetLocalTime > localTimeNow.AddHours(1)) 
        {
            targetLocalTime = targetLocalTime.AddDays(-1);
        }

        var targetUtcTime = targetLocalTime.AddHours(-5);
        var windowHours = int.Parse(_configuration["VoteSettings:ConfirmTimeWindowHours"] ?? "1");

        var result = await _voteService.ConfirmVoteAsync(dbUser.Id, last3, targetUtcTime, TimeSpan.FromHours(windowHours), cancellationToken);

        var reply = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
        await botClient.SendMessage(message.Chat.Id, reply, cancellationToken: cancellationToken);
    }
}
