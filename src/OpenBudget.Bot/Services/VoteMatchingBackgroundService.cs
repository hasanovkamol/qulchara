using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenBudget.Application.Services;

namespace OpenBudget.Bot.Services;

public class VoteMatchingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VoteMatchingBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10); // Foydalanuvchi "10 minutda ishlash yetarli" deb yozdi.

    public VoteMatchingBackgroundService(IServiceProvider serviceProvider, ILogger<VoteMatchingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VoteMatchingBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var voteService = scope.ServiceProvider.GetRequiredService<IVoteService>();

                _logger.LogInformation("Matching pending votes...");
                await voteService.MatchPendingVotesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing MatchPendingVotesAsync.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("VoteMatchingBackgroundService is stopping.");
    }
}
