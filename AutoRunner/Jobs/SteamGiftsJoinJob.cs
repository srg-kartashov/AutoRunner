using Giveaway.Application;

using Hangfire;
using Hangfire.MissionControl;
using Hangfire.RecurringJobExtensions;

using TelegramNotifier.Client;

namespace AutoRunner.Jobs;

[MissionLauncher(CategoryName = "SteamGifts")]
public sealed class SteamGiftsJoinJob
{
    private readonly ILogger<SteamGiftsJoinJob> _logger;
    private readonly ITelegramNotifier<SteamGiftsJoinJob> _telegramNotifier;
    private readonly SteamGiftsAutomationService _automationService;

    public SteamGiftsJoinJob(
        ILogger<SteamGiftsJoinJob> logger,
        ITelegramNotifier<SteamGiftsJoinJob> telegramNotifier,
        SteamGiftsAutomationService automationService)
    {
        _logger = logger;
        _telegramNotifier = telegramNotifier;
        _automationService = automationService;
    }

    [Mission(Name = "Join SteamGifts Giveaways", Description = "Filters, joins, and hides SteamGifts giveaways")]
    [JobDisplayName("SteamGifts: Process Giveaways")]
    public Task JoinGiveaways(IJobCancellationToken cancellationToken) =>
        JoinGiveaways(cancellationToken, withDelay: false);

    [RecurringJob("0 9,20 * * *", TimeZone = "FLE Standard Time", RecurringJobId = "SteamGifts: Process Giveaways")]
    [AutomaticRetry(Attempts = 0)]
    [JobDisplayName("SteamGifts: Process Giveaways")]
    public async Task JoinGiveaways(IJobCancellationToken cancellationToken, bool? withDelay)
    {
        if (withDelay ?? true)
            await WaitRandomDelayAsync(cancellationToken, TimeSpan.FromHours(1));

        await _telegramNotifier.SendTextAsync("Starting SteamGifts giveaway job...");

        try
        {
            var summary = await _automationService.RunAsync(cancellationToken.ShutdownToken);
            await _telegramNotifier.SendTextAsync(summary.ToDisplayText());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception in SteamGifts job");

            try
            {
                await _telegramNotifier.SendTextAsync($"SteamGifts job failed:\n<pre>{exception.Message}</pre>");
            }
            catch (Exception notificationException)
            {
                _logger.LogError(notificationException, "Failed to send SteamGifts error notification to Telegram");
            }

            throw;
        }
    }

    private async Task WaitRandomDelayAsync(IJobCancellationToken cancellationToken, TimeSpan maximumDelay)
    {
        var delay = TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * maximumDelay.TotalMilliseconds);
        _logger.LogInformation("Waiting {DelayMinutes:F1} minutes before the SteamGifts job", delay.TotalMinutes);
        await Task.Delay(delay, cancellationToken.ShutdownToken);
    }
}
