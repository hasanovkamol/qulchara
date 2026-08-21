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

using OpenBudget.Bot.Services;

namespace OpenBudget.Bot.Handlers;

public class AdminHandler
{
    private readonly IVoteService _voteService;
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly IBotSettingService _settingService;
    private readonly BrokerHandler _brokerHandler;
    private readonly SuperAdminHandler _superAdminHandler;
    private readonly IDocumentationService _docService;

    public AdminHandler(
        IVoteService voteService, 
        IConfiguration configuration, 
        IUserService userService, 
        IBotSettingService settingService,
        BrokerHandler brokerHandler,
        SuperAdminHandler superAdminHandler,
        IDocumentationService docService)
    {
        _voteService = voteService;
        _configuration = configuration;
        _userService = userService;
        _settingService = settingService;
        _brokerHandler = brokerHandler;
        _superAdminHandler = superAdminHandler;
        _docService = docService;
    }

    public static ReplyKeyboardMarkup GetMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("👥 Brokerlar ro'yxati"), new KeyboardButton("🚶‍♂️ Mehmonlar ro'yxati") },
            new[] { new KeyboardButton("🛡 Adminlar ro'yxati"), new KeyboardButton("📊 Brokerlar statistikasi") },
            new[] { new KeyboardButton("➕ Broker qo'shish"), new KeyboardButton("✅ Ovoz tasdiqlash") },
            new[] { new KeyboardButton("📨 Ommaviy xabar"), new KeyboardButton("✅ OB da tasdiqlanganlar") },
            new[] { new KeyboardButton("📜 SMS lar tarixi"), new KeyboardButton("📋 Mening ovozlarim") },
            new[] { new KeyboardButton("📝 Ovoz qo'shish"), new KeyboardButton("📊 Mening statistikam") },
            new[] { new KeyboardButton("ℹ️ Loyiha ma'lumotlari") }
        }) { ResizeKeyboard = true };
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, User dbUser, CancellationToken cancellationToken)
    {
        var text = message.Text?.Trim() ?? message.Caption?.Trim();
        var replyMarkup = GetMenuKeyboard();

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

        if (dbUser.BotState == BotState.WaitingForBroadcastMessage || dbUser.BotState == BotState.WaitingForDirectMessage)
        {
            await _superAdminHandler.HandleMessageAsync(botClient, message, dbUser, cancellationToken);
            return;
        }
        else if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (text == "📨 Ommaviy xabar" || text == "/broadcast")
        {
            await _superAdminHandler.HandleMessageAsync(botClient, message, dbUser, cancellationToken);
            return;
        }

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



        if (text == "👥 Brokerlar ro'yxati" || text == "/brokers")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await _superAdminHandler.SendBrokersPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text == "🚶‍♂️ Mehmonlar ro'yxati" || text == "/guests")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await _superAdminHandler.SendGuestsPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text == "🛡 Adminlar ro'yxati" || text == "/admins")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await _superAdminHandler.SendAdminsPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text == "➕ Broker qo'shish")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForBrokerIdentifier, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "➕ <b>Broker Qo'shish</b>\n\n" +
                      "Yangi brokerning <b>Telegram ID</b> sini (masalan <code>123456789</code>), <b>@username</b>ini (masalan <code>@foydalanuvchi</code>) yuboring yoki foydalanuvchi yuborgan xabarni ushbu chatga <b>Forward</b> qiling.",
                parseMode: ParseMode.Html,
                replyMarkup: BotConstants.GetCancelKeyboard(),
                cancellationToken: cancellationToken);
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
            
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📋 Jami ovozlar ro'yxati", "all_votes_1") }
            });

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: statsText,
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard,
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

        if (text == "✅ OB da tasdiqlanganlar" || text == "/pendingconfirmations")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            var pending = await _voteService.GetPendingConfirmationsAsync(cancellationToken);
            if (!pending.Any())
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "✅ Kutilayotgan ovoz tasdiqlari yo'q.",
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
                return;
            }

            var responseText = "✅ <b>Botga hali kiritilmagan SMS tasdiqlar:</b>\n\n";
            for (int i = 0; i < pending.Count; i++)
            {
                var p = pending[i];
                responseText += $"{i + 1}. <b>{p.LastNDigits}</b> ({p.TargetTime:HH:mm})\n";
            }
            responseText += "\n<i>Iltimos, ushbu SMS raqamli ovozlarni botga qo'shing.</i>";

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "📜 SMS lar tarixi" || text == "/smshistory")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await _superAdminHandler.SendConfirmationsHistoryPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
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
            var (docText, docKeyboard) = _docService.GetMainMenu(dbUser.Role);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: docText,
                parseMode: ParseMode.Html,
                replyMarkup: docKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (text != null && text.StartsWith("/")) return;

        if (dbUser.BotState == BotState.WaitingForConfirmation)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var digitsCount = await _settingService.GetLastDigitsCountAsync(cancellationToken);

            if (parts.Length < 2)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Noto'g'ri format. Format: <code>{new string('1', digitsCount)} 15:30</code>",
                    parseMode: ParseMode.Html,
                    replyMarkup: BotConstants.GetCancelKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var lastNDigits = parts[0];
            if (lastNDigits.Length != digitsCount || !long.TryParse(lastNDigits, out _))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Oxirgi {digitsCount} ta raqam noto'g'ri. Misol: {new string('1', digitsCount)}",
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

            var result = await _voteService.ConfirmVoteAsync(dbUser.Id, lastNDigits, targetLocalTime, TimeSpan.FromHours(windowHours), cancellationToken);

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
        else if (dbUser.BotState == BotState.WaitingForBrokerIdentifier)
        {
            string identifier = "";
            if (message.ForwardFrom != null)
            {
                identifier = message.ForwardFrom.Id.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                identifier = text;
            }

            var result = await _userService.PromoteBrokerByIdentifierAsync(identifier, cancellationToken);
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);

            var replyText = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            await botClient.SendMessage(message.Chat.Id, replyText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);

            if (result.Success && result.TargetUser != null)
            {
                try
                {
                    await botClient.SendMessage(
                        chatId: result.TargetUser.TelegramId,
                        text: "🎉 <b>Tabriklaymiz!</b> Sizga administrator tomonidan <b>Broker</b>lik ruxsati berildi.\n\nBotdan foydalanish uchun /start tugmasini bosing.",
                        parseMode: ParseMode.Html,
                        replyMarkup: BrokerHandler.GetMenuKeyboard(),
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // Ignore offline target user notification failure
                }
            }
            return;
        }
        else
        {
            await botClient.SendMessage(message.Chat.Id, "Iltimos, quyidagi menyu tugmalaridan foydalaning.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, User dbUser, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        if (string.IsNullOrEmpty(data)) return;

        if (data.StartsWith("approve_broker_"))
        {
            if (int.TryParse(data.Replace("approve_broker_", ""), out int targetUserId))
            {
                await _userService.PromoteToBrokerAsync(targetUserId, cancellationToken);
                var targetUser = await _userService.GetByIdAsync(targetUserId, cancellationToken);

                await botClient.AnswerCallbackQuery(callbackQuery.Id, "✅ Broker muvaffaqiyatli tasdiqlandi!", showAlert: true, cancellationToken: cancellationToken);

                try
                {
                    var updatedText = (callbackQuery.Message?.Text ?? "") + "\n\n✅ <b>Administrator tomonidan tasdiqlandi!</b>";
                    await botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, updatedText, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                }
                catch { }

                if (targetUser != null)
                {
                    try
                    {
                        await botClient.SendMessage(
                            chatId: targetUser.TelegramId,
                            text: "🎉 <b>Tabriklaymiz!</b> Sizning brokerlik so'rovingiz administrator tomonidan tasdiqlandi.\n\nBotdan foydalanish uchun /start tugmasini bosing.",
                            parseMode: ParseMode.Html,
                            replyMarkup: BrokerHandler.GetMenuKeyboard(),
                            cancellationToken: cancellationToken);
                    }
                    catch { }
                }
            }
            return;
        }
        else if (data.StartsWith("reject_broker_"))
        {
            if (int.TryParse(data.Replace("reject_broker_", ""), out int targetUserId))
            {
                var targetUser = await _userService.GetByIdAsync(targetUserId, cancellationToken);

                await botClient.AnswerCallbackQuery(callbackQuery.Id, "❌ Brokerlik so'rovi rad etildi.", showAlert: true, cancellationToken: cancellationToken);

                try
                {
                    var updatedText = (callbackQuery.Message?.Text ?? "") + "\n\n❌ <b>Administrator tomonidan rad etildi!</b>";
                    await botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, updatedText, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                }
                catch { }

                if (targetUser != null)
                {
                    try
                    {
                        await botClient.SendMessage(
                            chatId: targetUser.TelegramId,
                            text: "❌ Sizning brokerlik so'rovingiz administrator tomonidan rad etildi.",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                    catch { }
                }
            }
            return;
        }

        if (data.StartsWith("bpage_") || data.StartsWith("toggle_block_") || data.StartsWith("all_votes_") || data.StartsWith("sms_hist_") || data.StartsWith("apage_"))
        {
            await _superAdminHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
            return;
        }

        await _brokerHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
    }
}

