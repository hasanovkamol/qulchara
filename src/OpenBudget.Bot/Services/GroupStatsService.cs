using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace OpenBudget.Bot.Services;

public class GroupStatsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GroupStatsService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly HttpClient _httpClient;

    public GroupStatsService(
        IServiceProvider serviceProvider,
        ILogger<GroupStatsService> logger,
        ITelegramBotClient botClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botClient = botClient;
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("accept-language", "uz-UZ,uz;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("hl", "uz_lat");
        _httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Har soatning boshida ishlashi yoki hozirdan boshlab 1 soatda ishlashi mumkin.
        // Hozirgi talabga asosan har 1 soatda aylanadi.
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Keyingi ishga tushish vaqtini hisoblash (Masalan, har soatning boshida)
                // Yoki oddiygina 1 soat kutish:
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                
                await SendStatsToGroupsAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // stopped
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GroupStatsService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // xato bo'lsa 5 minutdan keyin qayta urinsin
            }
        }
    }

    private async Task SendStatsToGroupsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var telegramGroupService = scope.ServiceProvider.GetRequiredService<ITelegramGroupService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var groups = await telegramGroupService.GetActiveGroupsWithCodeAsync(cancellationToken);
        if (!groups.Any()) return;

        // Bot ichki statistikasi
        var internalStats = await userService.GetGlobalStatsAsync(cancellationToken);

        foreach (var group in groups)
        {
            try
            {
                int openBudgetCount = 0;
                
                // Tashqi API dan so'rov
                var url = $"https://new.openbudget.uz/api/v2/info/initiative/count/{group.InitiativeCode}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("count", out var countElement))
                        {
                            openBudgetCount = countElement.GetInt32();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse OpenBudget API response for code {Code}", group.InitiativeCode);
                    }
                }

                var text = $"📊 <b>Umumiy Ovozlar Statistikasi</b>\n" +
                           $"🆔 Tashabbus: <code>{group.InitiativeCode}</code>\n" +
                           $"━━━━━━━━━━━━━━━━━━\n" +
                           $"👥 <b>Jami brokerlar:</b> {internalStats.TotalBrokers}\n\n" +
                           $"📈 <b>Bot ichki statistikasi:</b>\n" +
                           $"   🔹 Jami ovozlar: <b>{internalStats.TotalVotes}</b>\n" +
                           $"   ✅ Tasdiqlangan: <b>{internalStats.ConfirmedVotes}</b>\n" +
                           $"   ❌ Rad etilgan: <b>{internalStats.RejectedVotes}</b>\n" +
                           $"   ⏳ Kutilayotgan: <b>{internalStats.PendingVotes}</b>\n\n" +
                           $"🌐 <b>OpenBudget saytidagi rasmiy ovozlar:</b> <b>{openBudgetCount}</b> ta\n" +
                           $"━━━━━━━━━━━━━━━━━━\n" +
                           $"<i>Avtomatik hisobot (har soatda yuboriladi)</i>";

                await _botClient.SendMessage(
                    chatId: group.ChatId,
                    text: text,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send stats to group {ChatId}", group.ChatId);
            }
        }
    }
}
