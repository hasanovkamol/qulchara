using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenBudget.Application.Services;
using Telegram.Bot;

namespace OpenBudget.Bot.Services;

public class ErrorNotificationService : INotificationService
{
    private readonly ITelegramBotClient _mainBotClient;
    private readonly ITelegramBotClient _errorBotClient;
    private readonly long _errorChatId;
    private readonly ILogger<ErrorNotificationService> _logger;

    public ErrorNotificationService(ITelegramBotClient mainBotClient, IConfiguration configuration, ILogger<ErrorNotificationService> logger)
    {
        _mainBotClient = mainBotClient;
        _logger = logger;
        
        var errorToken = configuration["TelegramBot:ErrorBotToken"];
        if (string.IsNullOrEmpty(errorToken))
        {
            // Fallback to main bot if no error token provided
            _errorBotClient = mainBotClient;
        }
        else
        {
            _errorBotClient = new TelegramBotClient(errorToken);
        }

        long.TryParse(configuration["TelegramBot:ErrorChatId"], out _errorChatId);
    }

    public async Task NotifyBrokerVoteConfirmedAsync(long brokerTelegramId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var text = $"🎉 Sizning {MaskPhoneNumber(phoneNumber)} raqamingiz tasdiqlandi!";
            await _mainBotClient.SendMessage(brokerTelegramId, text, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify broker {TelegramId}", brokerTelegramId);
        }
    }

    public async Task NotifyErrorAsync(Exception ex, long? userId = null, string? context = null, CancellationToken cancellationToken = default)
    {
        if (_errorChatId == 0) return;

        try
        {
            var text = $"🚨 ERROR | {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                       $"Type: {ex.GetType().Name}\n" +
                       $"Message: {ex.Message}\n" +
                       $"Source: {ex.Source ?? context ?? "Unknown"}\n" +
                       (userId.HasValue ? $"User TelegramId: {userId}\n" : "") +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            await _errorBotClient.SendMessage(_errorChatId, text, cancellationToken: cancellationToken);
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "Failed to send error notification.");
        }
    }

    public async Task NotifyInfoAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_errorChatId == 0) return;

        try
        {
            var text = $"ℹ️ INFO | {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                       $"{message}\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            await _errorBotClient.SendMessage(_errorChatId, text, cancellationToken: cancellationToken);
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "Failed to send info notification.");
        }
    }

    private string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length < 4) return phoneNumber;
        var last3 = phoneNumber.Substring(phoneNumber.Length - 3);
        return "+998***" + last3;
    }
}
