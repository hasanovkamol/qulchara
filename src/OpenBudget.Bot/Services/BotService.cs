using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using OpenBudget.Bot.Handlers;

namespace OpenBudget.Bot.Services;

public class BotService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotService> _logger;

    public BotService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<BotService> logger)
    {
        _botClient = botClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation($"Bot started @{me.Username}");

        try
        {
            await _botClient.SetMyCommands(
                new[]
                {
                    new Telegram.Bot.Types.BotCommand { Command = "start", Description = "Asosiy menyu" },
                    new Telegram.Bot.Types.BotCommand { Command = "request", Description = "Brokerlik so'rovi yuborish" },
                    new Telegram.Bot.Types.BotCommand { Command = "info", Description = "Loyiha ma'lumotlari" }
                },
                cancellationToken: stoppingToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set global bot commands.");
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
            await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling update");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram API Error");
        return Task.CompletedTask;
    }
}
