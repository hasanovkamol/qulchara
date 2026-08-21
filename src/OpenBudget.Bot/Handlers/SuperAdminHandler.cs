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

using System.IO;

using System.Collections.Concurrent;
using OpenBudget.Bot.Services;

namespace OpenBudget.Bot.Handlers;

public class SuperAdminHandler
{
    private static readonly ConcurrentDictionary<int, int> _directMessageTargets = new();

    private readonly IUserService _userService;
    private readonly IVoteService _voteService;
    private readonly ITelegramGroupService _groupService;
    private readonly IBotSettingService _settingService;
    private readonly IConfiguration _configuration;
    private readonly BrokerHandler _brokerHandler;
    private readonly IQrCodeService _qrCodeService;
    private readonly IDocumentationService _docService;
    private readonly OpenBudget.Application.Services.IBotCommandService _botCommandService;

    public SuperAdminHandler(
        IUserService userService, 
        IVoteService voteService, 
        ITelegramGroupService groupService,
        IBotSettingService settingService,
        IConfiguration configuration,
        BrokerHandler brokerHandler,
        IQrCodeService qrCodeService,
        IDocumentationService docService,
        OpenBudget.Application.Services.IBotCommandService botCommandService)
    {
        _userService = userService;
        _voteService = voteService;
        _groupService = groupService;
        _settingService = settingService;
        _configuration = configuration;
        _brokerHandler = brokerHandler;
        _qrCodeService = qrCodeService;
        _docService = docService;
        _botCommandService = botCommandService;
    }

    public static ReplyKeyboardMarkup GetMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("🌍 Global Statistika"), new KeyboardButton("🛡 Admin tayinlash") },
            new[] { new KeyboardButton("👥 Brokerlar ro'yxati"), new KeyboardButton("🚶‍♂️ Mehmonlar ro'yxati") },
            new[] { new KeyboardButton("🛡 Adminlar ro'yxati"), new KeyboardButton("📢 Ulangan guruhlar") },
            new[] { new KeyboardButton("➕ Broker qo'shish"), new KeyboardButton("⚙️ Sozlamalar") },
            new[] { new KeyboardButton("✅ Ovoz tasdiqlash"), new KeyboardButton("✅ OB da tasdiqlanganlar") },
            new[] { new KeyboardButton("📜 SMS lar tarixi"), new KeyboardButton("🔲 QR Kod yuborish") },
            new[] { new KeyboardButton("📨 Ommaviy xabar"), new KeyboardButton("📋 Mening ovozlarim") },
            new[] { new KeyboardButton("📝 Ovoz qo'shish"), new KeyboardButton("📊 Mening statistikam") },
            new[] { new KeyboardButton("ℹ️ Loyiha ma'lumotlari"), new KeyboardButton("⚙️ Bot Buyruqlari") }
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
            if (dbUser.BotState == BotState.WaitingForBroadcastMessage)
            {
                var allUsers = await _userService.GetAllUsersAsync(cancellationToken);
                var brokers = allUsers.Where(u => u.Role == UserRole.Broker && u.IsActive).ToList();
                int successCount = 0;

                foreach (var broker in brokers)
                {
                    try
                    {
                        await botClient.CopyMessage(
                            chatId: broker.TelegramId,
                            fromChatId: message.Chat.Id,
                            messageId: message.MessageId,
                            cancellationToken: cancellationToken);
                        successCount++;
                    }
                    catch { }
                }

                await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"✅ Xabar <b>{successCount}</b> ta brokerga muvaffaqiyatli yuborildi.",
                    parseMode: ParseMode.Html,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
                return;
            }
            else if (dbUser.BotState == BotState.WaitingForDirectMessage)
            {
                if (_directMessageTargets.TryGetValue(dbUser.Id, out int targetUserId))
                {
                    var targetUser = await _userService.GetByIdAsync(targetUserId, cancellationToken);
                    if (targetUser != null && targetUser.IsActive)
                    {
                        try
                        {
                            await botClient.CopyMessage(
                                chatId: targetUser.TelegramId,
                                fromChatId: message.Chat.Id,
                                messageId: message.MessageId,
                                cancellationToken: cancellationToken);
                            
                            await botClient.SendMessage(
                                chatId: message.Chat.Id,
                                text: $"✅ Xabar <b>{targetUser.FullName}</b> ga muvaffaqiyatli yuborildi.",
                                parseMode: ParseMode.Html,
                                replyMarkup: replyMarkup,
                                cancellationToken: cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendMessage(
                                chatId: message.Chat.Id,
                                text: $"❌ <b>{targetUser.FullName}</b> ga xabar yuborishda xatolik yuz berdi.\nSabab: {ex.Message}",
                                parseMode: ParseMode.Html,
                                replyMarkup: replyMarkup,
                                cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(message.Chat.Id, "❌ Broker topilmadi yoki bloklangan.", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                    }
                    _directMessageTargets.TryRemove(dbUser.Id, out _);
                }
                await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
                return;
            }
        }
        else if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (text == "/start")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Salom SuperAdmin! Quyidagi boshqaruv menyusidan foydalaning.",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }


        if (text == "⚙️ Bot Buyruqlari" || text == "/botcommands")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendBotCommandsPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text == "🌍 Global Statistika" || text == "/globalstats")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            var stats = await _userService.GetGlobalStatsAsync(cancellationToken);
            var statsText = $"🌍 <b>Global Statistika</b>\n" +
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

        if (text == "🛡 Admin tayinlash" || text == "/assignadmin")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForAdminId, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "🛡 <b>Admin tayinlash</b>\n\nYangi adminga aylantirmoqchi bo'lgan foydalanuvchining <b>Telegram ID</b> sini (masalan <code>123456789</code>) yoki <b>@username</b>ini (masalan <code>@foydalanuvchi</code>) yuboring:",
                parseMode: ParseMode.Html,
                replyMarkup: BotConstants.GetCancelKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "👥 Brokerlar ro'yxati" || text == "/brokers")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendBrokersPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text == "🚶‍♂️ Mehmonlar ro'yxati" || text == "/guests")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendGuestsPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
            return;
        }

        if (text == "🛡 Adminlar ro'yxati" || text == "/admins")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendAdminsPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
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

        if (text == "📢 Ulangan guruhlar" || text == "/groups")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendGroupsListAsync(botClient, message.Chat.Id, cancellationToken);
            return;
        }

        if (text == "⚙️ Sozlamalar" || text == "/settings")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await SendSettingsPageAsync(botClient, message.Chat.Id, cancellationToken);
            return;
        }

        if (text == "📨 Ommaviy xabar" || text == "/broadcast")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForBroadcastMessage, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "📨 <b>Barcha brokerlarga xabar yuborish</b>\n\nIltimos, xabar matnini kiriting (rasm/video qo'shsangiz ham bo'ladi):",
                parseMode: ParseMode.Html,
                replyMarkup: BotConstants.GetCancelKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "✅ Ovoz tasdiqlash" || text == "/confirm")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForConfirmation, cancellationToken);
            var now = OpenBudget.Application.Helpers.DateTimeHelper.UzbekistanNow;
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Tasdiqlash uchun oxirgi raqamlarni, vaqtni (va ixtiyoriy kunni) kiriting.\nFormat: <code>[Raqamlar] [Soat:Minut] [Kun]</code>\nMisollar: <code>{new string('1', 3)} {now:HH:mm}</code> yoki <code>{new string('1', 3)} {now:HH:mm} {now:dd}</code>",
                parseMode: ParseMode.Html,
                replyMarkup: BotConstants.GetCancelKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        if (text == "🔲 QR Kod yuborish")
        {
            await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForQrUrl, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "🔲 <b>QR Kod Yaratish va Yuborish</b>\n\nBarcha brokerlarga ommaviy yuboriladigan URL manzilni kiriting:\n(Masalan: <code>https://openbudget.uz/boards/1/projects/555</code>)",
                parseMode: ParseMode.Html,
                replyMarkup: BotConstants.GetCancelKeyboard(),
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
            await SendConfirmationsHistoryPageAsync(botClient, message.Chat.Id, 1, cancellationToken);
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

        if (dbUser.BotState == BotState.WaitingForAdminId)
        {
            var result = await _userService.AssignRoleByIdentifierAsync(text, UserRole.Admin, UserRole.SuperAdmin, cancellationToken);
            var reply = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            await botClient.SendMessage(message.Chat.Id, reply, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            return;
        }

        if (dbUser.BotState == BotState.WaitingForConfirmation)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var digitsCount = await _settingService.GetLastDigitsCountAsync(cancellationToken);

            if (parts.Length < 2)
            {
                var now = OpenBudget.Application.Helpers.DateTimeHelper.UzbekistanNow;
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Noto'g'ri format.\nFormat: <code>[Raqamlar] [Soat:Minut] [Kun(ixtiyoriy)]</code>\nMisol: <code>{new string('1', digitsCount)} {now:HH:mm}</code>",
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

            // Agar kiritilgan kun hozirgi kundan katta bo'lsa va hozir oyning boshi bo'lsa, ehtimol admin o'tgan oyni kirityapti
            if (day > localTimeNow.Day && localTimeNow.Day < 5)
            {
                var lastMonth = localTimeNow.AddMonths(-1);
                currentYear = lastMonth.Year;
                currentMonth = lastMonth.Month;
            }

            DateTime targetLocalTime;
            try
            {
                targetLocalTime = new DateTime(currentYear, currentMonth, day, time.Hours, time.Minutes, 0);
            }
            catch
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Kiritilgan kun yoki vaqt noto'g'ri (masalan fevralda 30-kun yo'q). Qaytadan kiriting.",
                    replyMarkup: BotConstants.GetCancelKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var windowHours = int.Parse(_configuration["VoteSettings:ConfirmTimeWindowHours"] ?? "1");

            var result = await _voteService.ConfirmVoteAsync(dbUser.Id, lastNDigits, targetLocalTime, TimeSpan.FromHours(windowHours), cancellationToken);

            var reply = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(message.Chat.Id, reply, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return;
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
        else if (dbUser.BotState == BotState.WaitingForVote)
        {
            var result = await _voteService.AddVoteAsync(dbUser.Id, text, OpenBudget.Application.Helpers.DateTimeHelper.UzbekistanNow, cancellationToken);
            var replyText = result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}";
            
            await _userService.UpdateStateAsync(dbUser.Id, BotState.Default, cancellationToken);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: replyText,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
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

        if (data.StartsWith("botcmd_page_"))
        {
            if (int.TryParse(data.Replace("botcmd_page_", ""), out int page))
            {
                await EditBotCommandsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
            return;
        }

        if (data.StartsWith("botcmd_info_"))
        {
            if (int.TryParse(data.Replace("botcmd_info_", ""), out int commandId))
            {
                await SendBotCommandInfoAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, commandId, cancellationToken);
            }
            return;
        }

        if (data.StartsWith("botcmd_toggle_"))
        {
            if (int.TryParse(data.Replace("botcmd_toggle_", ""), out int commandId))
            {
                var isNowActive = await _botCommandService.ToggleCommandStatusAsync(commandId, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, isNowActive ? "✅ Buyruq faollashtirildi!" : "❌ Buyruq o'chirildi!", cancellationToken: cancellationToken);
                await SendBotCommandInfoAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, commandId, cancellationToken);
            }
            return;
        }

        if (data.StartsWith("approve_broker_"))
        {
            if (int.TryParse(data.Replace("approve_broker_", ""), out int targetUserId))
            {
                await _userService.PromoteToBrokerAsync(targetUserId, cancellationToken);
                var targetUser = await _userService.GetByIdAsync(targetUserId, cancellationToken);

                await botClient.AnswerCallbackQuery(callbackQuery.Id, "✅ Broker muvaffaqiyatli tasdiqlandi!", showAlert: true, cancellationToken: cancellationToken);

                try
                {
                    var updatedText = (callbackQuery.Message?.Text ?? "") + "\n\n✅ <b>SuperAdmin tomonidan tasdiqlandi!</b>";
                    await botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, updatedText, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                }
                catch { }

                if (targetUser != null)
                {
                    try
                    {
                        await botClient.SendMessage(
                            chatId: targetUser.TelegramId,
                            text: "🎉 <b>Tabriklaymiz!</b> Sizning brokerlik so'rovingiz tasdiqlandi.\n\nBotdan foydalanish uchun /start tugmasini bosing.",
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
                    var updatedText = (callbackQuery.Message?.Text ?? "") + "\n\n❌ <b>SuperAdmin tomonidan rad etildi!</b>";
                    await botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, updatedText, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                }
                catch { }

                if (targetUser != null)
                {
                    try
                    {
                        await botClient.SendMessage(
                            chatId: targetUser.TelegramId,
                            text: "❌ Sizning brokerlik so'rovingiz rad etildi.",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                    catch { }
                }
            }
            return;
        }
        else if (data.StartsWith("admin_stats_"))
        {
            if (int.TryParse(data.Replace("admin_stats_", ""), out int targetUserId))
            {
                var targetUser = await _userService.GetByIdAsync(targetUserId, cancellationToken);
                if (targetUser != null)
                {
                    var stats = await _voteService.GetBrokerStatsAsync(targetUserId, cancellationToken);
                    var statsText = $"📊 <b>Broker statistikasi: {targetUser.FullName}</b>\n" +
                                    $"━━━━━━━━━━━━━━━━━━\n" +
                                    $"📋 Jami ovozlar: <b>{stats.TotalVotes}</b>\n" +
                                    $"✅ Tasdiqlangan: <b>{stats.ConfirmedVotes}</b>\n" +
                                    $"⏳ Kutilmoqda: <b>{stats.PendingVotes}</b>\n" +
                                    $"❌ Rad etilgan: <b>{stats.RejectedVotes}</b>\n" +
                                    $"━━━━━━━━━━━━━━━━━━";
                    
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "Ma'lumot yuklandi", cancellationToken: cancellationToken);
                    await botClient.SendMessage(
                        chatId: callbackQuery.Message!.Chat.Id,
                        text: statsText,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "Broker topilmadi.", showAlert: true, cancellationToken: cancellationToken);
                }
            }
            return;
        }
        else if (data.StartsWith("msg_broker_"))
        {
            if (int.TryParse(data.Replace("msg_broker_", ""), out int targetUserId))
            {
                var targetUser = await _userService.GetByIdAsync(targetUserId, cancellationToken);
                if (targetUser != null)
                {
                    await _userService.UpdateStateAsync(dbUser.Id, BotState.WaitingForDirectMessage, cancellationToken);
                    _directMessageTargets[dbUser.Id] = targetUserId;
                    
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                    await botClient.SendMessage(
                        chatId: callbackQuery.Message!.Chat.Id,
                        text: $"✉️ <b>{targetUser.FullName}</b> ga yubormoqchi bo'lgan xabaringizni kiriting (rasm yoki matn):",
                        parseMode: ParseMode.Html,
                        replyMarkup: BotConstants.GetCancelKeyboard(),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "Broker topilmadi.", showAlert: true, cancellationToken: cancellationToken);
                }
            }
            return;
        }
        else if (data.StartsWith("toggle_block_"))
        {
            var parts = data.Split('_');
            if (parts.Length >= 4 && int.TryParse(parts[2], out int targetUserId) && int.TryParse(parts[3], out int page))
            {
                var result = await _userService.ToggleUserBlockAsync(targetUserId, dbUser.Role, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, result.Message, showAlert: true, cancellationToken: cancellationToken);
                await EditBrokerOptionsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, targetUserId, page, cancellationToken);
                return;
            }
        }
        else if (data.StartsWith("bopts_"))
        {
            var parts = data.Split('_');
            if (parts.Length >= 3 && int.TryParse(parts[1], out int targetUserId) && int.TryParse(parts[2], out int page))
            {
                await EditBrokerOptionsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, targetUserId, page, cancellationToken);
            }
        }
        else if (data.StartsWith("bpage_"))
        {
            if (int.TryParse(data.Replace("bpage_", ""), out int page))
            {
                await EditBrokersPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
        }
        else if (data.StartsWith("gpage_"))
        {
            if (int.TryParse(data.Replace("gpage_", ""), out int page))
            {
                await EditGuestsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
        }
        else if (data.StartsWith("apage_"))
        {
            if (int.TryParse(data.Replace("apage_", ""), out int page))
            {
                await EditAdminsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
        }
        else if (data.StartsWith("make_broker_"))
        {
            if (int.TryParse(data.Replace("make_broker_", ""), out int targetUserId))
            {
                var result = await _userService.AssignRoleAsync(targetUserId, UserRole.Broker, dbUser.Role, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, result.Message, showAlert: true, cancellationToken: cancellationToken);
                
                if (result.Success)
                {
                    try
                    {
                        var targetUser = await _userService.GetByIdAsync(targetUserId, cancellationToken);
                        if (targetUser != null)
                        {
                            await botClient.SendMessage(
                                chatId: targetUser.TelegramId,
                                text: "🎉 <b>Tabriklaymiz!</b>\n\nSizga tizim administratori tomonidan <b>Broker</b> roli berildi. \nBot imkoniyatlaridan to'liq foydalanish uchun /start buyrug'ini yuboring.",
                                parseMode: ParseMode.Html,
                                cancellationToken: cancellationToken);
                        }
                    }
                    catch { }

                    await EditGuestsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, 1, cancellationToken);
                }
            }
        }
        else if (data.StartsWith("set_digits_"))
        {
            if (int.TryParse(data.Replace("set_digits_", ""), out int newCount))
            {
                await _settingService.SetLastDigitsCountAsync(newCount, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, $"✅ Ovoz tasdiqlash uchun oxirgi {newCount} ta raqam tekshiriladi!", showAlert: true, cancellationToken: cancellationToken);
                await EditSettingsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
            }
        }
        else if (data == "toggle_guest_reg")
        {
            var allowGuests = await _settingService.GetAllowGuestRegistrationAsync(cancellationToken);
            await _settingService.SetAllowGuestRegistrationAsync(!allowGuests, cancellationToken);
            var status = !allowGuests ? "yoqildi" : "o'chirildi";
            await botClient.AnswerCallbackQuery(callbackQuery.Id, $"✅ Yangi mehmonlarni qabul qilish {status}!", showAlert: true, cancellationToken: cancellationToken);
            await EditSettingsPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
        }
        else if (data.StartsWith("all_votes_"))
        {
            if (int.TryParse(data.Replace("all_votes_", ""), out int page))
            {
                await EditAllVotesPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
        }
        else if (data.StartsWith("sms_hist_"))
        {
            if (int.TryParse(data.Replace("sms_hist_", ""), out int page))
            {
                await EditConfirmationsHistoryPageAsync(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, page, cancellationToken);
            }
        }
        else if (data.StartsWith("page_"))
        {
            await _brokerHandler.HandleCallbackQueryAsync(botClient, callbackQuery, dbUser, cancellationToken);
        }
    }

    private async Task EditAllVotesPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        var (text, inlineKeyboard) = await GenerateAllVotesPageAsync(page, cancellationToken);
        await botClient.EditMessageText(
            chatId: chatId,
            messageId: messageId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    private async Task<(string Text, InlineKeyboardMarkup Keyboard)> GenerateAllVotesPageAsync(int page, CancellationToken cancellationToken)
    {
        int pageSize = 10;
        var pagedResult = await _voteService.GetAllVotesPagedAsync(page, pageSize, cancellationToken);

        string text;
        if (pagedResult.TotalCount == 0)
        {
            text = "📋 <b>Jami Ovozlar</b>\n\nHozircha tizimda ovozlar mavjud emas.";
        }
        else
        {
            text = $"📋 <b>Jami Ovozlar Ro'yxati</b>\n\nJami: <b>{pagedResult.TotalCount}</b> ta ovoz\n\n";

            int index = (page - 1) * pageSize + 1;
            foreach (var vote in pagedResult.Items)
            {
                var statusEmoji = vote.Status switch
                {
                    VoteStatus.Confirmed => "✅",
                    VoteStatus.Pending => "⏳",
                    VoteStatus.Rejected => "❌",
                    _ => "❔"
                };

                text += $"<b>{index}.</b> {vote.PhoneNumber} | {statusEmoji} ({vote.Status})\n" +
                        $"   👤 <i>Broker:</i> {vote.BrokerName}\n" +
                        $"   🕒 <i>{vote.VotedAt:dd-MM-yyyy HH:mm}</i>\n";
                if (vote.Status == VoteStatus.Rejected && !string.IsNullOrEmpty(vote.RejectReason))
                {
                    text += $"   ℹ️ <i>{vote.RejectReason}</i>\n";
                }
                text += "\n";
                index++;
            }
        }

        int totalPages = (int)Math.Ceiling(pagedResult.TotalCount / (double)pageSize);
        var buttons = new List<InlineKeyboardButton>();

        if (page > 1)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Oldingi", $"all_votes_{page - 1}"));
        }
        if (page < totalPages)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("Keyingi ➡️", $"all_votes_{page + 1}"));
        }

        var keyboardLayout = new List<InlineKeyboardButton[]>();
        if (buttons.Any())
        {
            keyboardLayout.Add(buttons.ToArray());
        }

        // Remove the back button if we don't have a specific place to return to, or return to page 1. Wait, there's no main menu to return to via inline.
        // If we want a refresh button:
        keyboardLayout.Add(new[] { InlineKeyboardButton.WithCallbackData("🔄 Yangilash", $"all_votes_{page}") });

        return (text, new InlineKeyboardMarkup(keyboardLayout));
    }

    private async Task SendGroupsListAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var groups = await _groupService.GetActiveGroupsAsync(cancellationToken);
        if (!groups.Any())
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "📢 <b>Bot hozircha hech qaysi guruhga qo'shilmagan.</b>\n\nBotni kerakli guruhga qo'shsangiz, u avtomatik ravishda ushbu ro'yxatda paydo bo'ladi.",
                parseMode: ParseMode.Html,
                replyMarkup: GetMenuKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        var text = $"📢 <b>Ulangan Guruhlar ro'yxati</b> ({groups.Count} ta)\n" +
                   $"━━━━━━━━━━━━━━━━━━\n";

        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            var usernameText = string.IsNullOrEmpty(g.Username) ? "" : $" (@{g.Username})";
            text += $"{i + 1}. <b>{g.Title}</b>{usernameText}\n" +
                    $"   🆔 Chat ID: <code>{g.ChatId}</code>\n" +
                    $"   📅 Qo'shilgan: {g.JoinedAt:dd.MM.yyyy HH:mm}\n\n";

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🔄 {g.Title} ni sinxronlash", $"sync_group_{g.ChatId}")
            });
        }

        text += $"━━━━━━━━━━━━━━━━━━\n" +
                $"💡 <i>Guruh ma'murlarini tizimga ro'yxatga olish uchun yuqoridagi 'Sinxronlash' tugmasini bosing.</i>";

        var markup = new InlineKeyboardMarkup(buttons);

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }

    public async Task SendConfirmationsHistoryPageAsync(ITelegramBotClient botClient, long chatId, int page, CancellationToken cancellationToken)
    {
        var (text, inlineKeyboard) = await GenerateConfirmationsHistoryPageAsync(page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    public async Task EditConfirmationsHistoryPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        var (text, inlineKeyboard) = await GenerateConfirmationsHistoryPageAsync(page, cancellationToken);
        await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    private async Task<(string Text, InlineKeyboardMarkup Keyboard)> GenerateConfirmationsHistoryPageAsync(int page, CancellationToken cancellationToken)
    {
        int pageSize = 10;
        var pagedResult = await _voteService.GetConfirmationHistoryPagedAsync(page, pageSize, cancellationToken);

        string text;
        if (pagedResult.TotalCount == 0)
        {
            text = "📜 <b>SMS lar tarixi</b>\n\nHozircha tizimda SMS tasdiqlar tarixi mavjud emas.";
        }
        else
        {
            text = $"📜 <b>SMS lar tarixi</b>\n\nJami: <b>{pagedResult.TotalCount}</b> ta\n\n";

            int index = (page - 1) * pageSize + 1;
            foreach (var item in pagedResult.Items)
            {
                var statusEmoji = item.Status switch
                {
                    VoteConfirmationStatus.Confirmed => "✅",
                    VoteConfirmationStatus.Pending => "⏳",
                    VoteConfirmationStatus.Rejected => "❌",
                    _ => "❔"
                };

                text += $"<b>{index}.</b> {item.LastNDigits} ({item.TargetTime:HH:mm}) | {statusEmoji}\n";
                if (item.Status == VoteConfirmationStatus.Confirmed)
                {
                    var maskedPhone = item.PhoneNumber != null && item.PhoneNumber.Length >= 4 
                        ? "+998***" + item.PhoneNumber.Substring(item.PhoneNumber.Length - 3) 
                        : item.PhoneNumber;
                    text += $"   👤 <i>Tasdiqlangan:</i> {item.BrokerName} ({maskedPhone})\n";
                }
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

    public async Task SendAdminsPageAsync(ITelegramBotClient botClient, long chatId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateAdminsPageAsync(page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    public async Task EditAdminsPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        try
        {
            var (text, markup) = await GenerateAdminsPageAsync(page, cancellationToken);
            await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
        }
        catch { }
    }

    public async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateAdminsPageAsync(int page, CancellationToken cancellationToken)
    {
        var allUsers = await _userService.GetAllUsersAsync(cancellationToken);
        var admins = allUsers.Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin).OrderByDescending(u => u.CreatedAt).ToList();

        if (!admins.Any())
        {
            return ("🛡 <b>Adminlar topilmadi.</b>", null);
        }

        int pageSize = 5;
        var totalPages = (int)Math.Ceiling((double)admins.Count / pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pagedAdmins = admins.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var text = $"🛡 <b>Adminlar Ro'yxati</b> (Sahifa {page}/{totalPages}, Jami: {admins.Count} ta)\n" +
                   $"━━━━━━━━━━━━━━━━━━\n";

        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < pagedAdmins.Count; i++)
        {
            var a = pagedAdmins[i];
            var index = (page - 1) * pageSize + i + 1;
            var usernameText = string.IsNullOrEmpty(a.Username) ? "" : $" (@{a.Username})";
            var statusBadge = a.IsActive ? "🟢 <b>Faol</b>" : "🔴 <b>Bloklangan</b>";
            var roleBadge = a.Role == UserRole.SuperAdmin ? "👑 SuperAdmin" : "🛡 Admin";

            text += $"{index}. <b>{a.FullName ?? "Noma'lum"}</b>{usernameText}\n" +
                    $"   🆔 ID: <code>{a.Id}</code> | 📞 TG ID: <code>{a.TelegramId}</code>\n" +
                    $"   ⚡️ Holati: {statusBadge} | {roleBadge}\n\n";

            var shortName = a.FullName ?? a.Username ?? a.Id.ToString();
            string maxName = shortName.Length > 15 ? shortName.Substring(0, 15) + ".." : shortName;

            var actionIcon = a.IsActive ? "🚫 Bloklash" : "✅ Faollashtirish";
            
            // Allow SuperAdmin to block/unblock Admin, but do not allow to block another SuperAdmin via this UI 
            // (or let it be, the service might handle logic, but here we can just skip if it's SuperAdmin)
            if (a.Role == UserRole.Admin)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"{actionIcon} ({maxName})", $"toggle_block_{a.Id}") });
            }
        }

        var navButtons = new List<InlineKeyboardButton>();
        if (page > 1) navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"apage_{page - 1}"));
        navButtons.Add(InlineKeyboardButton.WithCallbackData($"🔄 {page}/{totalPages}", $"apage_{page}"));
        if (page < totalPages) navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"apage_{page + 1}"));

        buttons.Add(navButtons.ToArray());

        return (text, new InlineKeyboardMarkup(buttons));
    }

    public async Task SendBrokersPageAsync(ITelegramBotClient botClient, long chatId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateBrokersPageAsync(page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    public async Task EditBrokersPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        try
        {
            var (text, markup) = await GenerateBrokersPageAsync(page, cancellationToken);
            await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
        }
        catch
        {
            // Fallback for message not modified or editing exceptions
        }
    }

    public async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateBrokersPageAsync(int page, CancellationToken cancellationToken)
    {
        var allUsers = await _userService.GetAllUsersAsync(cancellationToken);
        var brokers = allUsers.Where(u => u.Role == UserRole.Broker).OrderByDescending(u => u.CreatedAt).ToList();

        if (!brokers.Any())
        {
            return ("👥 <b>Brokerlar topilmadi.</b>", null);
        }

        int pageSize = 5;
        var totalPages = (int)Math.Ceiling((double)brokers.Count / pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pagedBrokers = brokers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var text = $"👥 <b>Brokerlar Ro'yxati</b> (Sahifa {page}/{totalPages}, Jami: {brokers.Count} ta)\n" +
                   $"━━━━━━━━━━━━━━━━━━\n";

        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < pagedBrokers.Count; i++)
        {
            var b = pagedBrokers[i];
            var index = (page - 1) * pageSize + i + 1;
            var usernameText = string.IsNullOrEmpty(b.Username) ? "" : $" (@{b.Username})";
            var statusBadge = b.IsActive ? "🟢 <b>Faol</b>" : "🔴 <b>Bloklangan</b>";

            text += $"{index}. <b>{b.FullName ?? "Noma'lum"}</b>{usernameText}\n" +
                    $"   🆔 ID: <code>{b.Id}</code> | 📞 TG ID: <code>{b.TelegramId}</code>\n" +
                    $"   ⚡️ Holati: {statusBadge}\n\n";

            var shortName = b.FullName ?? b.Username ?? b.Id.ToString();
            if (shortName.Length > 15) shortName = shortName.Substring(0, 12) + "...";

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"⚙️ Boshqarish ({shortName})", $"bopts_{b.Id}_{page}")
            });
        }

        text += $"━━━━━━━━━━━━━━━━━━";

        var prevPage = page > 1 ? page - 1 : 1;
        var nextPage = page < totalPages ? page + 1 : totalPages;

        var prevButton = page > 1 ? InlineKeyboardButton.WithCallbackData("◀️ Oldingi", $"bpage_{prevPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");
        var currButton = InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "noop");
        var nextButton = page < totalPages ? InlineKeyboardButton.WithCallbackData("Keyingi ▶️", $"bpage_{nextPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");

        buttons.Add(new[] { prevButton, currButton, nextButton });

        var markup = new InlineKeyboardMarkup(buttons);
        return (text, markup);
    }
    public async Task EditBrokerOptionsPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int brokerId, int returnPage, CancellationToken cancellationToken)
    {
        try
        {
            var (text, markup) = await GenerateBrokerOptionsPageAsync(brokerId, returnPage, cancellationToken);
            await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
        }
        catch
        {
            // Fallback for message not modified or editing exceptions
        }
    }

    public async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateBrokerOptionsPageAsync(int brokerId, int returnPage, CancellationToken cancellationToken)
    {
        var targetUser = await _userService.GetByIdAsync(brokerId, cancellationToken);
        if (targetUser == null)
        {
            return ("❌ <b>Foydalanuvchi topilmadi.</b>", new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔙 Orqaga", $"bpage_{returnPage}")));
        }

        var usernameText = string.IsNullOrEmpty(targetUser.Username) ? "" : $" (@{targetUser.Username})";
        var statusBadge = targetUser.IsActive ? "🟢 <b>Faol</b>" : "🔴 <b>Bloklangan</b>";

        var text = $"⚙️ <b>Broker Boshqaruvi</b>\n" +
                   $"━━━━━━━━━━━━━━━━━━\n" +
                   $"👤 Ism: <b>{targetUser.FullName ?? "Noma'lum"}</b>{usernameText}\n" +
                   $"🆔 Tizim ID: <code>{targetUser.Id}</code>\n" +
                   $"📞 Telegram ID: <code>{targetUser.TelegramId}</code>\n" +
                   $"⚡️ Holati: {statusBadge}\n" +
                   $"📅 Qo'shilgan: {targetUser.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                   $"━━━━━━━━━━━━━━━━━━";

        var buttons = new List<InlineKeyboardButton[]>();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"📊 Statistika", $"admin_stats_{targetUser.Id}"),
            InlineKeyboardButton.WithCallbackData($"✉️ Xabar yozish", $"msg_broker_{targetUser.Id}")
        });

        var blockBtnText = targetUser.IsActive ? "🔴 Bloklash" : "🟢 Blokdan chiqarish";
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(blockBtnText, $"toggle_block_{targetUser.Id}_{returnPage}")
        });

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Orqaga", $"bpage_{returnPage}")
        });

        var markup = new InlineKeyboardMarkup(buttons);
        return (text, markup);
    }

    public async Task SendGuestsPageAsync(ITelegramBotClient botClient, long chatId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateGuestsPageAsync(page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    public async Task EditGuestsPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        try
        {
            var (text, markup) = await GenerateGuestsPageAsync(page, cancellationToken);
            await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
        }
        catch
        {
            // Fallback for message not modified or editing exceptions
        }
    }

    public async Task<(string Text, InlineKeyboardMarkup? Markup)> GenerateGuestsPageAsync(int page, CancellationToken cancellationToken)
    {
        var allUsers = await _userService.GetAllUsersAsync(cancellationToken);
        var guests = allUsers.Where(u => u.Role == UserRole.Guest).OrderByDescending(u => u.CreatedAt).ToList();

        if (!guests.Any())
        {
            return ("🚶‍♂️ <b>Mehmonlar topilmadi.</b>", null);
        }

        int pageSize = 5;
        var totalPages = (int)Math.Ceiling((double)guests.Count / pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pagedGuests = guests.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var text = $"🚶‍♂️ <b>Mehmonlar Ro'yxati</b> (Sahifa {page}/{totalPages}, Jami: {guests.Count} ta)\n" +
                   $"━━━━━━━━━━━━━━━━━━\n";

        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < pagedGuests.Count; i++)
        {
            var g = pagedGuests[i];
            var index = (page - 1) * pageSize + i + 1;
            var usernameText = string.IsNullOrEmpty(g.Username) ? "" : $" (@{g.Username})";

            text += $"{index}. <b>{g.FullName ?? "Noma'lum"}</b>{usernameText}\n" +
                    $"   🆔 ID: <code>{g.Id}</code> | 📞 TG ID: <code>{g.TelegramId}</code>\n" +
                    $"   📅 Kirdi: {g.CreatedAt:dd.MM.yyyy HH:mm}\n\n";

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"✅ Broker qilish", $"make_broker_{g.Id}")
            });
        }

        text += $"━━━━━━━━━━━━━━━━━━";

        var prevPage = page > 1 ? page - 1 : 1;
        var nextPage = page < totalPages ? page + 1 : totalPages;

        var prevButton = page > 1 ? InlineKeyboardButton.WithCallbackData("◀️ Oldingi", $"gpage_{prevPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");
        var currButton = InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "noop");
        var nextButton = page < totalPages ? InlineKeyboardButton.WithCallbackData("Keyingi ▶️", $"gpage_{nextPage}") : InlineKeyboardButton.WithCallbackData("🚫", "noop");

        buttons.Add(new[] { prevButton, currButton, nextButton });

        var markup = new InlineKeyboardMarkup(buttons);
        return (text, markup);
    }

    public async Task SendSettingsPageAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var (text, markup) = await GenerateSettingsPageAsync(cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    public async Task EditSettingsPageAsync(ITelegramBotClient botClient, long chatId, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            var (text, markup) = await GenerateSettingsPageAsync(cancellationToken);
            await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
        }
        catch
        {
            // Ignore message not modified
        }
    }

    private async Task<(string Text, InlineKeyboardMarkup Markup)> GenerateSettingsPageAsync(CancellationToken cancellationToken)
    {
        var count = await _settingService.GetLastDigitsCountAsync(cancellationToken);
        var allowGuests = await _settingService.GetAllowGuestRegistrationAsync(cancellationToken);

        var guestStatusText = allowGuests ? "✅ Yoqilgan" : "❌ O'chirilgan";

        var text = $"⚙️ <b>Tizim Sozlamalari</b>\n" +
                   $"━━━━━━━━━━━━━━━━━━\n" +
                   $"👥 Yangi mehmonlarni qabul qilish: <b>{guestStatusText}</b>\n\n" +
                   $"📱 Ovoz tasdiqlashda tekshiriladigan oxirgi raqamlar soni: <b>{count} ta</b>\n\n" +
                   $"💡 <i>SMS kelganda telefon raqamining oxirgi necha xonasi bo'yicha qidirilishini tanlang:</i>";

        var buttons = new List<InlineKeyboardButton[]>();
        
        // Digits setting
        var digitsRow = new List<InlineKeyboardButton>();
        for (int i = 2; i <= 5; i++)
        {
            var label = i == count ? $"✅ {i} ta" : $"{i} ta";
            digitsRow.Add(InlineKeyboardButton.WithCallbackData(label, $"set_digits_{i}"));
        }
        buttons.Add(digitsRow.ToArray());

        // Guest toggle setting
        var guestToggleLabel = allowGuests ? "❌ Qabulni o'chirish" : "✅ Qabulni yoqish";
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(guestToggleLabel, "toggle_guest_reg") });

        return (text, new InlineKeyboardMarkup(buttons));
    }

    private async Task SendBotCommandsPageAsync(ITelegramBotClient botClient, long chatId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GetBotCommandsPageContentAsync(page, cancellationToken);
        await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    private async Task EditBotCommandsPageAsync(ITelegramBotClient botClient, long chatId, int messageId, int page, CancellationToken cancellationToken)
    {
        var (text, markup) = await GetBotCommandsPageContentAsync(page, cancellationToken);
        await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    private async Task<(string Text, InlineKeyboardMarkup Markup)> GetBotCommandsPageContentAsync(int page, CancellationToken cancellationToken)
    {
        int pageSize = 10;
        var (commands, total) = await _botCommandService.GetCommandsPagedAsync(page, pageSize, cancellationToken);
        
        string text = $"⚙️ <b>Bot Buyruqlari</b> (Sahifa {page})\nBoshqarish uchun ℹ️ Info tugmasini bosing.";
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var cmd in commands)
        {
            var statusIcon = cmd.IsActive ? "🟢" : "🔴";
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"{statusIcon} {cmd.CommandText}", $"botcmd_info_{cmd.Id}"),
                InlineKeyboardButton.WithCallbackData("ℹ️ Info", $"botcmd_info_{cmd.Id}")
            });
        }

        var navRow = new List<InlineKeyboardButton>();
        if (page > 1) navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Oldingi", $"botcmd_page_{page - 1}"));
        if (page * pageSize < total) navRow.Add(InlineKeyboardButton.WithCallbackData("Keyingi ➡️", $"botcmd_page_{page + 1}"));
        
        if (navRow.Any()) buttons.Add(navRow.ToArray());

        return (text, new InlineKeyboardMarkup(buttons));
    }

    private async Task SendBotCommandInfoAsync(ITelegramBotClient botClient, long chatId, int messageId, int commandId, CancellationToken cancellationToken)
    {
        var command = await _botCommandService.GetCommandByIdAsync(commandId, cancellationToken);
        if (command == null) return;

        string statusText = command.IsActive ? "🟢 Faol" : "🔴 O'chirilgan";
        string text = $"📌 <b>Buyruq:</b> {command.CommandText}\n\n" +
                      $"📝 <b>Tavsif:</b> {command.Description}\n" +
                      $"👥 <b>Kimlar ishlata oladi:</b> {command.AllowedRoles}\n" +
                      $"⚙️ <b>Holat:</b> {statusText}";

        var buttons = new List<InlineKeyboardButton[]>();
        
        string toggleBtnText = command.IsActive ? "🔴 O'chirish" : "🟢 Faollashtirish";
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(toggleBtnText, $"botcmd_toggle_{command.Id}") });
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Orqaga", "botcmd_page_1") }); // Returns to page 1

        await botClient.EditMessageText(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
    }
}


