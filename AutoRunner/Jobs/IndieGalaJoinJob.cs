using AutoRunner.Factories;

using Hangfire;
using Hangfire.MissionControl;

using IndieGala.Client;

using Newtonsoft.Json.Linq;

using SteamGifts.Client;

using SteamPowered.Client;

using TelegramNotifier.Client;

namespace AutoRunner.Jobs
{
    [MissionLauncher(CategoryName = "IndieGala")]
    public class IndieGalaJoinJob
    {
        private readonly ILogger<IndieGalaJoinJob> _logger;
        //private readonly ITelegramNotifier<SteamGiftsJoinJob> _telegramNotifier;
        private readonly ISteamPoweredClient _steamPoweredClient;
        private readonly IPlaywrightDriverFactory _playwrightDriverFactory;
        private readonly string _sessionId;

        public IndieGalaJoinJob(ILogger<IndieGalaJoinJob> logger,
          //ITelegramNotifier<SteamGiftsJoinJob> telegramNotifier,
          IConfiguration configuration,
          ISteamPoweredClient steamPoweredClient,
          IPlaywrightDriverFactory playwrightDriverFactory)
        {
            _logger = logger;
            //_telegramNotifier = telegramNotifier;
            _steamPoweredClient = steamPoweredClient;
            _playwrightDriverFactory = playwrightDriverFactory;
            _sessionId = configuration["IndieGala:SessionId"] ?? throw new ArgumentNullException("IndieGala sessionId is not configured");
        }

        [Mission(Name = "Join IndieGala Giveaways", Description = "Automatically joins available IndieGala giveaways")]
        [AutomaticRetry(Attempts = 0)]
        [JobDisplayName("IndieGala: Auto Join Giveaways")]
        public async Task JoinGiveaways(IJobCancellationToken cancellationToken)
        {
            await using var ctx = await _playwrightDriverFactory.CreateContextAsync();
            var page = ctx.Page;

            try
            {
                var indieGalaClient = new IndieGalaClient(page, _logger);
                await indieGalaClient.AuthAsync(_sessionId);
                var user = await indieGalaClient.GetUserInfoAsync();
                var giveaways = await indieGalaClient.GetAllGiveawaysAsync();
                var joinedGiveaways = giveaways.Where(g => g.Joined).ToList();
                var currentPoints = user.Points;
                foreach (var giveaway in giveaways)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogInformation("🎯 Processing giveaway: {GameName}, AppId: {AppId}, Required points: {Points}, Current points: {CurrentPoints}",
                        giveaway.GameName, giveaway.ApplicationId, giveaway.Points, currentPoints);

                    if(giveaway.Level > user.Level)
                    {
                        _logger.LogInformation("🔒 Skipped giveaway due to insufficient level: {GameName} (Required: {Level}, Current: {UserLevel})",
                            giveaway.GameName, giveaway.Level, user.Level);
                        continue;
                    }

                    if (giveaway.Joined)
                    {
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
                    var joinResult = await indieGalaClient.JoinGiveawayAsync(giveaway.GiveawayUrl);
                    if (joinResult)
                    {
                        _logger.LogInformation("✅ Successfully joined giveaway: {GameName} (Points: {Points})", giveaway.GameName, giveaway.Points);
                        currentPoints -= giveaway.Points;
                    }
                    else
                    {
                        _logger.LogWarning("❌ Failed to join giveaway: {GameName}", giveaway.GameName);
                    }

                    await Task.Delay(2000);
                }
                ;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate with SteamGifts");
                return;
            }
        }
    }
}
