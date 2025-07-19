using Microsoft.Extensions.Logging;

using OpenQA.Selenium;

using SteamGifts.Client.Models;
using SteamGifts.Client.Pages.SteamGift;

namespace SteamGifts.Client
{
    public class SteamGiftsClient
    {
        private readonly IWebDriver _driver;
        private readonly ILogger<SteamGiftsClient>? _logger;

        public SteamGiftsClient(IWebDriver driver, ILogger<SteamGiftsClient>? logger = null)
        {
            _driver = driver;
            _logger = logger;
        }

        public UserInfo GetUserInfo()
        {
            var page = new SteamGiftPage(_driver);
            page.GoToPage(1);
            var userInfo = new UserInfo
            {
                Username = page.GetUserName(),
                Points = page.GetPoints(),
                Level = page.GetLevel()
            };
            return userInfo;
        }

        public IEnumerable<Giveaway> GetAllGiveaways()
        {
            var page = new SteamGiftPage(_driver);
            var result = new List<Giveaway>();
            int currentPage = 1;
            do
            {
                _logger?.LogDebug("Loading page {PageNumber}", currentPage);
                page.GoToPage(currentPage++);
                var giveaways = page.GetGiveaways();
                var giveawaysData = giveaways.Select(g => new Giveaway
                {
                    GameName = g.GetGameName(),
                    GiveawayUrl = g.GetGiveawayUrl(),
                    SteamUrl = g.GetSteamUrl(),
                    Points = g.GetPoints(),
                    Level = g.GetLevel(),
                    ApplicationId = g.GetApplicationId(),
                    Joined = g.HasAlreadyJoined(),
                });
                result.AddRange(giveawaysData);
            }
            while (page.IsNextPageAvailable());

            _logger?.LogInformation("Collected {Count} giveaways", result.Count);
            return result;
        }

        public bool JoinGiveaway(string giveawayUrl)
        {
            var page = new GiveawayPage(_driver, giveawayUrl);
            page.GoToPage();
            var success = page.PerformEnter();
            return success;
        }
    }
}
