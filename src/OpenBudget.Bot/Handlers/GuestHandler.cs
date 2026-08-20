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

using OpenBudget.Bot.Services;

namespace OpenBudget.Bot.Handlers;

public class GuestHandler
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly IDocumentationService _docService;

    public GuestHandler(IUserService userService, IConfiguration configuration, IDocumentationService docService)
    {
        _userService = userService;
        _configuration = configuration;
        _docService = docService;
    }

    public static ReplyKeyboardMarkup GetMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📩 Brokerlik so'rovi yuborish") },
            new[] { new KeyboardButton("ℹ️ Loyiha ma'lumotlari") }
        }) { ResizeKeyboard = true };
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, User dbUser, CancellationToken cancellationToken)
    {
        var text = message.Text?.Trim();
        var replyMarkup = GetMenuKeyboard();

        if (text == "/start")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "⚠️ <b>Hush kelibsiz!</b>\n\nSiz hali tizimda <b>Broker</b> sifatida ro'yxatdan o'tmagansiz.\nOvoz kiritish imkoniyatiga ega bo'lish uchun quyidagi <b>📩 Brokerlik so'rovi yuborish</b> tugmasini bosing.",
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }



        if (text == "📩 Brokerlik so'rovi yuborish" || text == "/request")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForBrokerRequestInfo, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "📝 <b>Brokerlik So'rovi</b>\n\nIltimos, o'zingiz haqingizda qisqacha ma'lumot yuboring (Ism, Telefon raqam va loyiha haqida izoh):",
                parseMode: ParseMode.Html,
                replyMarkup: BotConstants.GetCancelKeyboard(),
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

        if (dbUser.BotState == BotState.WaitingForBrokerRequestInfo && !string.IsNullOrEmpty(text))
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "✅ <b>So'rovingiz administratorlarga yuborildi!</b>\n\nSo'rovingiz ko'rib chiqilgach, sizga bildirishnoma keladi.",
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);

            // Notify SuperAdmins and Admins
            await NotifyAdminsAboutRequestAsync(botClient, dbUser, text, cancellationToken);
            return;
        }

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "⚠️ Siz hali broker sifatida ro'yxatdan o'tmagansiz. So'rov yuborish uchun <b>📩 Brokerlik so'rovi yuborish</b> tugmasini bosing.",
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task NotifyAdminsAboutRequestAsync(ITelegramBotClient botClient, User guestUser, string infoText, CancellationToken cancellationToken)
    {
        var allUsers = await _userService.GetAllUsersAsync(cancellationToken);
        var admins = allUsers.Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin).ToList();

        var superAdminIds = _configuration.GetSection("TelegramBot:SuperAdminIds").Get<long[]>() ?? Array.Empty<long>();

        var adminTelegramIds = admins.Select(u => u.TelegramId).Union(superAdminIds).Distinct().ToList();

        var text = $"📩 <b>Yangi Brokerlik So'rovi</b>\n" +
                   $"━━━━━━━━━━━━━━━━━━\n" +
                   $"👤 Foydalanuvchi: <b>{guestUser.FullName ?? guestUser.Username ?? guestUser.TelegramId.ToString()}</b>\n" +
                   $"🆔 Telegram ID: <code>{guestUser.TelegramId}</code>\n" +
                   $"Username: @{guestUser.Username ?? "yo'q"}\n" +
                   $"📝 Ma'lumot: <i>{infoText}</i>\n" +
                   $"━━━━━━━━━━━━━━━━━━";

        var markup = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Tasdiqlash", $"approve_broker_{guestUser.Id}"),
                InlineKeyboardButton.WithCallbackData("❌ Rad etish", $"reject_broker_{guestUser.Id}")
            }
        });

        foreach (var adminId in adminTelegramIds)
        {
            try
            {
                await botClient.SendMessage(
                    chatId: adminId,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: markup,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                // Ignore failure to send to offline admin
            }
        }
    }
}
