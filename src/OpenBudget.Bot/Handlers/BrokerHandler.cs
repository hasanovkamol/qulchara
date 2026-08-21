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
            new[] { new KeyboardButton("✅ Mening tasdiqlanganlarim") },
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
                text: "Salom, Broker!\n\nOvoz kiritish va statistikani ko'rish uchun quyidagi menyudan foydalaning.\n\nEslatma: Ovozlar rasmiy OpenBudget tizimida ro'yxatga olinadi, bot esa siz to'plagan ovozlarni kuzatib boradi.",
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

        if (text == "✅ Mening tasdiqlanganlarim" || text == "/myconfirmations")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendConfirmationsHistoryPageAsync(botClient, message.Chat.Id, dbUser.Id, 1, cancellationToken);
            return;
        }

        if (text == "📝 Ovoz qo'shish" || text == "/vote")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForVote, cancellationToken);
            var now = OpenBudget.Application.Helpers.DateTimeHelper.UzbekistanNow;
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Ovoz berish uchun 9 xonali telefon raqamni va ixtiyoriy ravishda vaqtni (kun bilan) kiriting.\nFormat: <code>[TelefonNomer] [Soat:Minut] [Kun]</code>\nMisol: <code>901234567 {now:HH:mm}</code> yoki <code>901234567 {now:HH:mm} {now:dd}</code>\n+998 avtomatik qo'shiladi.",
                parseMode: ParseMode.Html,
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
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string rawPhoneNumber = parts[0];
            var localTimeNow = OpenBudget.Application.Helpers.DateTimeHelper.UzbekistanNow;
            DateTime votedAt = localTimeNow;

            if (parts.Length >= 2)
            {
                if (TimeSpan.TryParse(parts[1], out TimeSpan time))
                {
                    int day = localTimeNow.Day;
                    if (parts.Length >= 3)
                    {
                        if (!int.TryParse(parts[2], out day) || day < 1 || day > 31)
                        {
                            await botClient.SendMessage(
                                chatId: message.Chat.Id,
                                text: "Sana (kun) noto'g'ri kiritildi. Misol: 01 dan 31 gacha.",
                                replyMarkup: BotConstants.GetCancelKeyboard(),
                                cancellationToken: cancellationToken);
                            return;
                        }
                    }

                    int currentYear = localTimeNow.Year;
                    int currentMonth = localTimeNow.Month;

                    if (day > localTimeNow.Day && localTimeNow.Day < 5)
                    {
                        var lastMonth = localTimeNow.AddMonths(-1);
                        currentYear = lastMonth.Year;
                        currentMonth = lastMonth.Month;
                    }

                    try
                    {
                        votedAt = new DateTime(currentYear, currentMonth, day, time.Hours, time.Minutes, 0);
                    }
                    catch
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "Kiritilgan kun yoki vaqt noto'g'ri. Qaytadan kiriting.",
                            replyMarkup: BotConstants.GetCancelKeyboard(),
                            cancellationToken: cancellationToken);
                        return;
                    }
                }
                else
                {
                    // Agar faqat telefon raqam ko'p qismli kiritilsa (masalan 90 123 45 67)
                    rawPhoneNumber = string.Join("", parts);
                }
            }

            var result = await _voteService.AddVoteAsync(dbUser.Id, rawPhoneNumber, votedAt, cancellationToken);
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
        else if (data.StartsWith("sms_hist_"))
        {
            if (int.TryParse(data.Replace("sms_hist_", ""), out int page))
            {
                await EditConfirmationsHistoryPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, dbUser.Id, page, cancellationToken);
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

    public async Task SendConfirmationsHistoryPageAsync(ITelegramBotClient botClient, long chatId, int brokerId, int page, CancellationToken cancellationToken)
    {
        var (text, inlineKeyboard) = await GenerateConfirmationsHistoryPageAsync(brokerId, page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    public async Task EditConfirmationsHistoryPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int brokerId, int page, CancellationToken cancellationToken)
    {
        var (text, inlineKeyboard) = await GenerateConfirmationsHistoryPageAsync(brokerId, page, cancellationToken);
        await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    private async Task<(string Text, InlineKeyboardMarkup Keyboard)> GenerateConfirmationsHistoryPageAsync(int brokerId, int page, CancellationToken cancellationToken)
    {
        int pageSize = 10;
        var pagedResult = await _voteService.GetBrokerConfirmationHistoryPagedAsync(brokerId, page, pageSize, cancellationToken);

        string text;
        if (pagedResult.TotalCount == 0)
        {
            text = "✅ <b>Mening tasdiqlanganlarim</b>\n\nHozircha sizda tasdiqlangan SMSlar tarixi mavjud emas.";
        }
        else
        {
            text = $"✅ <b>Mening tasdiqlanganlarim</b>\n\nJami: <b>{pagedResult.TotalCount}</b> ta\n\n";

            int index = (page - 1) * pageSize + 1;
            foreach (var item in pagedResult.Items)
            {
                text += $"<b>{index}.</b> {item.LastNDigits} ({item.TargetTime:HH:mm}) | ✅\n";
                var maskedPhone = item.PhoneNumber != null && item.PhoneNumber.Length >= 4 
                    ? "+998***" + item.PhoneNumber.Substring(item.PhoneNumber.Length - 3) 
                    : item.PhoneNumber;
                text += $"   📱 <i>Raqam:</i> {maskedPhone}\n";
                text += $"   🕐 <i>Kiritildi: {item.CreatedAt:dd-MM-yyyy HH:mm}</i>\n\n";
                index++;
            }
        }

        int totalPages = (int)Math.Ceiling(pagedResult.TotalCount / (double)pageSize);
        var buttons = new List<InlineKeyboardButton>();

        if (page > 1)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Oldingi", $"sms_hist_{page - 1}"));
        }
        if (page < totalPages)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("Keyingi ➡️", $"sms_hist_{page + 1}"));
        }

        var keyboardLayout = new List<InlineKeyboardButton[]>();
        if (buttons.Any())
        {
            keyboardLayout.Add(buttons.ToArray());
        }

        keyboardLayout.Add(new[] { InlineKeyboardButton.WithCallbackData("🔄 Yangilash", $"sms_hist_{page}") });

        return (text, new InlineKeyboardMarkup(keyboardLayout));
    }
}

