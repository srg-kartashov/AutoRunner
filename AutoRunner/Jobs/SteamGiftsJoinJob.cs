using AutoRunner.Factories;

using Hangfire;
using Hangfire.MissionControl;

using OpenQA.Selenium;

using SteamGifts.Client;

using SteamPowered.Client;

using System.Text;

using TelegramNotifier.Client;

namespace AutoRunner.Jobs
{
    [MissionLauncher(CategoryName = "SteamGift")]
    public class SteamGiftsJoinJob
    {
        private readonly ILogger<SteamGiftsJoinJob> _logger;
        private readonly ITelegramNotifier<SteamGiftsJoinJob> _telegramNotifier;
        private readonly ISteamPoweredClient _steamPoweredClient;
        private readonly ISeleniumDriverFactory _seleniumDriverFactory;
        private readonly string _token;

        public SteamGiftsJoinJob(ILogger<SteamGiftsJoinJob> logger,
            ITelegramNotifier<SteamGiftsJoinJob> telegramNotifier,
            IConfiguration configuration,
            ISteamPoweredClient steamPoweredClient,
            ISeleniumDriverFactory seleniumDriverFactory)
        {
            _logger = logger;
            _telegramNotifier = telegramNotifier;
            _steamPoweredClient = steamPoweredClient;
            _seleniumDriverFactory = seleniumDriverFactory;
            _token = configuration["SteamGifts:Token"] ?? throw new ArgumentNullException("SteamGifts token is not configured");
        }

        [Mission(Name = "Join SteamGifts Giveaways", Description = "Automatically joins available SteamGifts giveaways")]
        [AutomaticRetry(Attempts = 0)]
        [JobDisplayName("SteamGifts: Auto Join Giveaways")]
        public async Task JoinGiveaways()
        {
            await _telegramNotifier.SendTextAsync("Starting SteamGifts giveaway join job...");
            using var driver = _seleniumDriverFactory.CreateDriver();


            try
            {
                var steamGiftsClient = new SteamGiftsClient(driver, _logger);
                steamGiftsClient.InjectCookies([new Cookie("PHPSESSID", _token, "www.steamgifts.com", "/", null)]);
                var user = steamGiftsClient.GetUserInfo();
                var giveaways = steamGiftsClient.GetAllGiveaways().ToList();
                var currentPoints = user.Points;

                foreach (var giveaway in giveaways)
                {
                    _logger.LogInformation("🎯 Processing giveaway: {GameName}, AppId: {AppId}, Required points: {Points}, Current points: {CurrentPoints}",
                        giveaway.GameName, giveaway.ApplicationId, giveaway.Points, currentPoints);

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
                    var joinResult = steamGiftsClient.JoinGiveaway(giveaway.GiveawayUrl);
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SteamGifts job");

                try
                {
                    await _telegramNotifier.SendTextAsync($"❌ Exception in SteamGifts job:\n```\n{ex.Message}\n```");

                    // Скриншот
                    if (driver is ITakesScreenshot screenshotTaker)
                    {
                        var screenshot = screenshotTaker.GetScreenshot();
                        var imageBytes = screenshot.AsByteArray;
                        await _telegramNotifier.SendScreenshotAsync(imageBytes, "🖼️ Screenshot at exception");
                    }

                    // HTML страницы
                    var html = driver.PageSource;
                    var htmlBytes = Encoding.UTF8.GetBytes(html);
                    using var htmlStream = new MemoryStream(htmlBytes);
                    await _telegramNotifier.SendFileAsync(htmlStream, "page.html", "📄 Page HTML at exception");

                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "Failed to send error notification to Telegram");
                }

                throw;
            }
        }
    }
}
