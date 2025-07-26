using AutoRunner.Factories;

using Hangfire;
using Hangfire.MissionControl;
using Hangfire.RecurringJobExtensions;

using IndieGala.Client;
using IndieGala.Client.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using SteamPowered.Client;

using System.Text;

using TelegramNotifier.Client;

namespace AutoRunner.Jobs
{
    [MissionLauncher(CategoryName = "IndieGala")]
    public class IndieGalaJoinJob
    {
        private readonly ILogger<IndieGalaJoinJob> _logger;
        private readonly ITelegramNotifier<IndieGalaJoinJob> _telegramNotifier;
        private readonly ISteamPoweredClient _steamPoweredClient;
        private readonly IPlaywrightDriverFactory _playwrightDriverFactory;
        private readonly string _sessionId;

        public IndieGalaJoinJob(ILogger<IndieGalaJoinJob> logger,
          ITelegramNotifier<IndieGalaJoinJob> telegramNotifier,
          IConfiguration configuration,
          ISteamPoweredClient steamPoweredClient,
          IPlaywrightDriverFactory playwrightDriverFactory)
        {
            _logger = logger;
            _telegramNotifier = telegramNotifier;
            _steamPoweredClient = steamPoweredClient;
            _playwrightDriverFactory = playwrightDriverFactory;
            _sessionId = configuration["IndieGala:SessionId"] ?? throw new ArgumentNullException("IndieGala sessionId is not configured");
        }


        [Mission(Name = "Join IndieGala Giveaways", Description = "Automatically joins available IndieGala giveaways")]
        [JobDisplayName("IndieGala: Auto Join Giveaways")]
        public async Task JoinGiveaways(IJobCancellationToken cancellationToken)
        {
            await JoinGiveaways(cancellationToken, false);
        }

        [RecurringJob("0 8,19 * * *", TimeZone = "FLE Standard Time", RecurringJobId = "IndieGala: Auto Join Giveaways")]
        [AutomaticRetry(Attempts = 0)]
        [JobDisplayName("IndieGala: Auto Join Giveaways")]
        public async Task JoinGiveaways(IJobCancellationToken cancellationToken, bool? withDelay)
        {
            if (withDelay ?? true)
            {
                await WaitRandomDelayAsync(cancellationToken, TimeSpan.FromHours(1));
            }

            await _telegramNotifier.SendTextAsync("🟢 Starting IndieGala giveaway join job...");
            var stats = new GiveawayStats();

            await using var ctx = await _playwrightDriverFactory.CreateContextAsync();
            var page = ctx.Page;

            try
            {
                var indieGalaClient = new IndieGalaClient(page, _logger);
                await indieGalaClient.AuthAsync(_sessionId);
                var user = await indieGalaClient.GetUserInfoAsync();
                var giveaways = await indieGalaClient.GetAllGiveawaysAsync();
                var currentPoints = user.Points;

                _logger.LogInformation("📝 Fetched {Count} giveaways from IndieGala", giveaways.Count());

                List<(IndieGalaGiveaway Giveaway, double Rating, double TotalReviews, double Score)> giveawayToReviews = new();
               
                foreach (var g in giveaways)
                {
                    if (string.IsNullOrEmpty(g.ApplicationId))
                        _logger.LogWarning("⚠️ Giveaway {GameName} has no ApplicationId, skipping review fetch", g.GameName);

                    var review = await _steamPoweredClient.GetAppReviewsAsync(g.ApplicationId);
                    if (review != null)
                    {
                        var score = review.Rating * Math.Log10(review.TotalReviews + 1);
                        giveawayToReviews.Add((g, review.Rating, review.TotalReviews, review.Rating * score));
                        _logger.LogInformation("📊 Fetched reviews for {GameName}: {Rating:F2}%, Total reviews: {TotalReviews}",
                            g.GameName, review.Rating, review.TotalReviews);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to fetch reviews for game: {GameName}, AppId: {AppId}",
                              g.GameName, g.ApplicationId);
                        continue;
                    }
                }

               

                foreach (var giveawayToReview in giveawayToReviews.OrderByDescending(e=>e.Score))
                {
                    var giveaway = giveawayToReview.Giveaway;

                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogInformation("🎯 Processing giveaway: {GameName}, AppId: {AppId}, Required points: {Points}, Current points: {CurrentPoints}",
                        giveaway.GameName, giveaway.ApplicationId, giveaway.Points, currentPoints);
                    _logger.LogInformation("📊 Review for {GameName}: {Rating:F2}%, Total reviews: {TotalReviews}, Score: {Score}",
                      giveaway.GameName, giveawayToReview.Rating, giveawayToReview.TotalReviews, giveawayToReview.Score);

                    if (giveaway.Level > user.Level)
                    {
                        _logger.LogInformation("🔒 Skipped giveaway due to insufficient level: {GameName} (Required: {Level}, Current: {UserLevel})",
                            giveaway.GameName, giveaway.Level, user.Level);
                        continue;
                    }

                    if (giveaway.Joined)
                    {
                        stats.AlreadyJoined++;
                        _logger.LogInformation("🔒 Already joined giveaway: {GameName}", giveaway.GameName);
                        continue;
                    }

                    if (giveaway.IsCollection)
                    {
                        _logger.LogInformation("🔁 Skipped collection giveaway: {GameName}", giveaway.GameName);
                        continue;
                    }

                    if (currentPoints < giveaway.Points)
                    {
                        stats.InsufficientPoints++;
                        _logger.LogInformation("⛔ Not enough points to join: {GameName} (Required: {Points}, Available: {CurrentPoints})",
                            giveaway.GameName, giveaway.Points, currentPoints);
                        continue;
                    }

                    _logger.LogDebug("🟢 Sufficient points, trying to join giveaway: {GameName}", giveaway.GameName);
                    var joinResult = await indieGalaClient.JoinGiveawayAsync(giveaway.GiveawayUrl);
                    if (joinResult)
                    {
                        stats.Joined++;
                        _logger.LogInformation("✅ Successfully joined giveaway: {GameName} (Points: {Points})", giveaway.GameName, giveaway.Points);
                        currentPoints -= giveaway.Points;
                    }
                    else
                    {
                        stats.FailedJoins++;
                        _logger.LogWarning("❌ Failed to join giveaway: {GameName}", giveaway.GameName);
                    }

                    await Task.Delay(2000);
                }
                await SendSummaryAsync(stats, currentPoints);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, page);
                throw;
            }
        }

        private async Task SendSummaryAsync(GiveawayStats stats, int remainingPoints)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🎉 <b>IndieGala Giveaway Join Summary</b>");
            sb.AppendLine($"🧾 Total giveaways: <b>{stats.Total}</b>");
            sb.AppendLine($"✅ Joined: <b>{stats.Joined}</b>");
            sb.AppendLine($"🔁 Skipped collections: <b>{stats.SkippedCollections}</b>");
            sb.AppendLine($"🔒 Already joined: <b>{stats.AlreadyJoined}</b>");
            sb.AppendLine($"⛔ Not enough points: <b>{stats.InsufficientPoints}</b>");
            sb.AppendLine($"❌ Failed to join: <b>{stats.FailedJoins}</b>");
            sb.AppendLine($"🎯 Remaining points: <b>{remainingPoints}</b>");

            await _telegramNotifier.SendTextAsync(sb.ToString());
        }

        private async Task HandleErrorAsync(Exception ex, IPage page)
        {
            _logger.LogError(ex, "Unhandled exception in SteamGifts job");

            try
            {
                await _telegramNotifier.SendTextAsync($"❌ Exception in SteamGifts job:\n```\n{ex.Message}\n```");

                var screenshotBytes = await page.ScreenshotAsync(new() { FullPage = true });
                await _telegramNotifier.SendScreenshotAsync(screenshotBytes, "🖼️ Screenshot at exception");

                var html = await page.ContentAsync();
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                using var htmlStream = new MemoryStream(htmlBytes);
                await _telegramNotifier.SendFileAsync(htmlStream, "page.html", "📄 Page HTML at exception");
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "Failed to send error notification to Telegram");
            }
        }

        private async Task WaitRandomDelayAsync(IJobCancellationToken cancellationToken, TimeSpan delayTime)
        {
            var random = new Random();
            int delayMilliseconds = random.Next(0, (int)delayTime.TotalMilliseconds);
            var delay = TimeSpan.FromMilliseconds(delayMilliseconds);

            _logger.LogInformation("⏳ Waiting for {DelayMinutes} minutes before starting giveaway job...", delay.TotalMinutes);

            await Task.Delay(delay, cancellationToken.ShutdownToken);
        }
    }
}
