using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.Services;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
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

        var webAppUrl = _configuration["MiniApp:Url"] ?? "";
        var replyMarkup = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("🌍 Global Statistika"), new KeyboardButton("🛡 Admin tayinlash") },
            new[] { new KeyboardButton("📉 Brokerlar ro'yxati"), new KeyboardButton("📱 Mini App") { WebApp = new WebAppInfo { Url = webAppUrl } } }
        }) { ResizeKeyboard = true };

        if (text == "/start")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Salom SuperAdmin! Quyidagi menyudan foydalaning.",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "🌍 Global Statistika" || text == "/globalstats")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
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
            await botClient.SendMessage(message.Chat.Id, statsText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return;
        }

        if (text == "🛡 Admin tayinlash")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.WaitingForAdminId, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, "Yangi adminga aylantirmoqchi bo'lgan foydalanuvchining ID raqamini kiriting:", cancellationToken: cancellationToken);
            return;
        }

        if (text == "📉 Brokerlar ro'yxati")
        {
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            await SendBrokersPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text.StartsWith("/")) return;

        if (dbUser.BotState == Domain.Enums.BotState.WaitingForAdminId)
        {
            if (int.TryParse(text, out int targetUserId))
            {
                var result = await _userService.AssignRoleAsync(targetUserId, Domain.Enums.UserRole.Admin, Domain.Enums.UserRole.SuperAdmin, cancellationToken);
                var reply = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
                await botClient.SendMessage(message.Chat.Id, reply, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(message.Chat.Id, "Noto'g'ri ID kiritildi.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            await _userService.UpdateStateAsync(dbUser.Id, Domain.Enums.BotState.Default, cancellationToken);
            return;
        }

        await botClient.SendMessage(message.Chat.Id, "Iltimos, tugmalardan foydalaning.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, User dbUser, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        if (string.IsNullOrEmpty(data)) return;

        if (data.StartsWith("bpage_"))
        {
            if (int.TryParse(data.Replace("bpage_", ""), out int page))
            {
                await EditBrokersPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
        }
    }

    private async Task SendBrokersPageAsync(ITelegramBotClient botClient, long chatId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateBrokersPageAsync(page, cancellationToken);
        await botClient.SendMessage(chatId, text, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    private async Task EditBrokersPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateBrokersPageAsync(page, cancellationToken);
        await botClient.EditMessageText(chatId, messageId, text, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    private async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateBrokersPageAsync(int page, CancellationToken cancellationToken)
    {
        var allUsers = await _userService.GetAllUsersAsync(cancellationToken);
        var brokers = allUsers.Where(u => u.Role == Domain.Enums.UserRole.Broker).OrderByDescending(u => u.CreatedAt).ToList();

        if (!brokers.Any())
        {
            return ("Brokerlar topilmadi.", null);
        }

        int pageSize = 1;
        var totalPages = (int)System.Math.Ceiling((double)brokers.Count / pageSize);
        var pagedBrokers = brokers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var broker = pagedBrokers.FirstOrDefault();

        if (broker == null)
            return ("Xatolik yuz berdi.", null);

        var text = $"📉 Brokerlar Ro'yxati {page}/{totalPages}\n" +
                   $"━━━━━━━━━━━━━━━━━━\n" +
                   $"🆔 ID: {broker.Id}\n" +
                   $"👤 Ism: {broker.FullName ?? "Noma'lum"}\n" +
                   $"📞 TelegramId: {broker.TelegramId}\n" +
                   $"📅 Sana: {broker.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}\n" +
                   $"━━━━━━━━━━━━━━━━━━";

        var prevPage = page > 1 ? page - 1 : 1;
        var nextPage = page < totalPages ? page + 1 : totalPages;

        var prevButton = page > 1 ? InlineKeyboardButton.WithCallbackData("◀️ Oldingi", $"bpage_{prevPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");
        var currButton = InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "noop");
        var nextButton = page < totalPages ? InlineKeyboardButton.WithCallbackData("Keyingi ▶️", $"bpage_{nextPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");

        var markup = new InlineKeyboardMarkup(new[]
        {
            new[] { prevButton, currButton, nextButton }
        });

        return (text, markup);
    }
}
