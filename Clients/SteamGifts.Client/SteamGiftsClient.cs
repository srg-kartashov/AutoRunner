using Microsoft.Extensions.Logging;

using OpenQA.Selenium;

using SteamGifts.Client.Models;
using SteamGifts.Client.Pages.SteamGift;

namespace SteamGifts.Client
{
    public class SteamGiftsClient
    {
        private readonly IWebDriver _driver;
        private readonly ILogger? _logger;
        private const int DefaultWaitTime = 5000; // Default wait time in milliseconds

        public SteamGiftsClient(IWebDriver driver, ILogger? logger = null)
        {
            _driver = driver;
            _logger = logger;
        }

        public void InjectCookies(IEnumerable<Cookie> cookies)
        {
            _driver.Navigate().GoToUrl("https://www.steamgifts.com");
            foreach (var cookie in cookies)
                _driver.Manage().Cookies.AddCookie(cookie);
            _driver.Navigate().Refresh();
        }

        public UserInfo GetUserInfo()
        {
            var page = new SteamGiftPage(_driver);
            page.GoToPage(1);
            Thread.Sleep(DefaultWaitTime);
            if (page.IsAuthorized() == false)
            {
                _logger?.LogWarning("User is not authorized on SteamGifts");
                throw new UnauthorizedAccessException("User is not authorized on SteamGifts");
            }

            var userInfo = new UserInfo
            {
                Username = page.GetUserName(),
                Points = page.GetPoints(),
                Level = page.GetLevel()
            };

            _logger?.LogInformation("User info: {Username}, Level {Level}, Points {Points}",
                userInfo.Username, userInfo.Level, userInfo.Points);

            return userInfo;
        }

        public IEnumerable<Giveaway> GetAllGiveaways()
        {
            var page = new SteamGiftPage(_driver);
            var result = new List<Giveaway>();
            int currentPage = 1;

           
            do
            {
                _logger?.LogDebug("Loading giveaways from page {Page}", currentPage);
                page.GoToPage(currentPage++);

                Thread.Sleep(DefaultWaitTime);

                if (page.IsConsentButtonVisible())
                {
                    _logger?.LogInformation("Consent button is visible, clicking it.");
                    page.ClickConsentButtonIfExists();
                    Thread.Sleep(DefaultWaitTime);
                }

                _logger?.LogDebug("Loaded giveaways from page {Page}", currentPage);
                var giveaways = page.GetGiveaways();
                Thread.Sleep(DefaultWaitTime);
                var giveawaysData = giveaways.Select(g => new Giveaway
                {
                    GameName = g.GetGameName(),
                    GiveawayUrl = g.GetGiveawayUrl(),
                    SteamUrl = g.GetSteamUrl(),
                    Points = g.GetPoints(),
                    Level = g.GetLevel(),
                    ApplicationId = g.GetApplicationId(),
                    Joined = g.HasAlreadyJoined(),
                    IsCollection = g.IsCollection()
                });
                result.AddRange(giveawaysData);
            }
            while (page.IsNextPageAvailable());

            _logger?.LogInformation("Collected total {Count} giveaways", result.Count);
            return result;
        }

        public bool JoinGiveaway(string giveawayUrl)
        {
            var page = new GiveawayPage(_driver, giveawayUrl);

            _logger?.LogDebug("Trying to join giveaway: {Url}", giveawayUrl);

            page.GoToPage();
            Thread.Sleep(DefaultWaitTime);

            bool result = page.PerformEnter();

            _logger?.LogInformation("Joined giveaway {Url}: {Result}", giveawayUrl, result);

            return result;
        }
    }
}
