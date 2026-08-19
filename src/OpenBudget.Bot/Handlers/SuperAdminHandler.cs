using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = OpenBudget.Domain.Entities.User;

namespace OpenBudget.Bot.Handlers;

public class SuperAdminHandler
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public SuperAdminHandler(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
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
                text: "Salom SuperAdmin! Siz admin panel orqali broker va adminlarga rol bera olasiz. WebApp-ni ochish uchun pastdagi tugmani bosing.",
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "/globalstats")
        {
            var stats = await _userService.GetGlobalStatsAsync(cancellationToken);
            var statsText = $"🌍 Global Statistika\n" +
                            $"━━━━━━━━━━━━━━━━━━\n" +
                            $"👥 Brokerlar soni: {stats.TotalBrokers}\n" +
                            $"🛡 Adminlar soni: {stats.TotalAdmins}\n" +
                            $"📋 Jami ovozlar: {stats.TotalVotes}\n" +
                            $"✅ Tasdiqlangan: {stats.ConfirmedVotes}\n" +
                            $"⏳ Kutilmoqda: {stats.PendingVotes}\n" +
                            $"❌ Rad etilgan: {stats.RejectedVotes}\n" +
                            $"━━━━━━━━━━━━━━━━━━";
            await botClient.SendMessage(message.Chat.Id, statsText, cancellationToken: cancellationToken);
            return;
        }

        // For other commands like /assign admin <userid>, we can do it via API (WebApp).
        await botClient.SendMessage(message.Chat.Id, "Boshqaruv uchun Mini App dan foydalaning.", cancellationToken: cancellationToken);
    }
}
