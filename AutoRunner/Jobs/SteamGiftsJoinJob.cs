using AutoRunner.Factories;

using Hangfire;
using Hangfire.MissionControl;

using Microsoft.Playwright;

using SteamGifts.Client;

using SteamPowered.Client;

using System.Text;

using TelegramNotifier.Client;

namespace AutoRunner.Jobs
{
    public class GiveawayStats
    {
        public int Total { get; set; }
        public int Joined { get; set; }
        public int SkippedCollections { get; set; }
        public int AlreadyJoined { get; set; }
        public int InsufficientPoints { get; set; }
        public int FailedJoins { get; set; }
    }

    [MissionLauncher(CategoryName = "SteamGift")]
    public class SteamGiftsJoinJob
    {
        private readonly ILogger<SteamGiftsJoinJob> _logger;
        private readonly ITelegramNotifier<SteamGiftsJoinJob> _telegramNotifier;
        private readonly ISteamPoweredClient _steamPoweredClient;
        private readonly IPlaywrightDriverFactory _playwrightDriverFactory;
        private readonly string _token;

        public SteamGiftsJoinJob(ILogger<SteamGiftsJoinJob> logger,
            ITelegramNotifier<SteamGiftsJoinJob> telegramNotifier,
            IConfiguration configuration,
            ISteamPoweredClient steamPoweredClient,
            IPlaywrightDriverFactory playwrightDriverFactory)
        {
            _logger = logger;
            _telegramNotifier = telegramNotifier;
            _steamPoweredClient = steamPoweredClient;
            _playwrightDriverFactory = playwrightDriverFactory;
            _token = configuration["SteamGifts:Token"] ?? throw new ArgumentNullException("SteamGifts token is not configured");
        }

        [Mission(Name = "Join SteamGifts Giveaways", Description = "Automatically joins available SteamGifts giveaways")]
        [AutomaticRetry(Attempts = 0)]
        [JobDisplayName("SteamGifts: Auto Join Giveaways")]
        public async Task JoinGiveaways(IJobCancellationToken cancellationToken)
        {
            await _telegramNotifier.SendTextAsync("🟢 Starting SteamGifts giveaway join job...");
            var stats = new GiveawayStats();
            await using var ctx = await _playwrightDriverFactory.CreateContextAsync();
            var page = ctx.Page;

            try
            {
                var steamGiftsClient = new SteamGiftsClient(page, _logger);
                await steamGiftsClient.AuthAsync(_token);
                var user = await steamGiftsClient.GetUserInfoAsync();
                var giveaways = await steamGiftsClient.GetAllGiveawaysAsync();
                stats.Total = giveaways.Count();
                var currentPoints = user.Points;

                foreach (var giveaway in giveaways)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogInformation("🎯 Processing giveaway: {GameName}, AppId: {AppId}, Required points: {Points}, Current points: {CurrentPoints}",
                        giveaway.GameName, giveaway.ApplicationId, giveaway.Points, currentPoints);

                    if (giveaway.Joined)
                    {
                        stats.AlreadyJoined++;
                        _logger.LogInformation("🔒 Already joined giveaway: {GameName}", giveaway.GameName);
                        continue;
                    }

                    if (giveaway.IsCollection)
                    {
                        stats.SkippedCollections++;
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

                    var reviews = await _steamPoweredClient.GetAppReviewsAsync(giveaway.ApplicationId);
                    if (reviews == null)
                    {
                        _logger.LogWarning("⚠️ Failed to fetch reviews for game: {GameName}, AppId: {AppId}",
                             giveaway.GameName, giveaway.ApplicationId);
                        continue;
                    }

                    _logger.LogInformation("📊 Review for {GameName}: {Rating:F2}%, Total reviews: {TotalReviews}",
                        giveaway.GameName, reviews.Rating, reviews.TotalReviews);

                    _logger.LogDebug("🟢 Sufficient points, trying to join giveaway: {GameName}", giveaway.GameName);
                    var joinResult = await steamGiftsClient.JoinGiveawayAsync(giveaway.GiveawayUrl);
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
            sb.AppendLine("🎉 <b>SteamGifts Giveaway Join Summary</b>");
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
    }
}
