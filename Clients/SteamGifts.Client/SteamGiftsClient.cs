using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using SteamGifts.Client.Models;
using SteamGifts.Client.Pages;

namespace SteamGifts.Client
{
    public class SteamGiftsClient
    {
        private readonly IPage _page;
        private readonly ILogger? _logger;
        private const int DefaultWaitTime = 1000; // Default wait time in milliseconds
        private string _baseUrl = "https://www.steamgifts.com";

        public SteamGiftsClient(IPage page, ILogger? logger = null)
        {
            _page = page;
            _logger = logger;
        }

        public async Task AuthAsync(string tocken)
        {
            await _page.GotoAsync("https://www.steamgifts.com");
            var context = _page.Context;
            await context.ClearCookiesAsync();
            var phpsessidCookie = new Cookie { Name = "PHPSESSID", Value = tocken, Domain = "www.steamgifts.com", Path = "/" };
            await context.AddCookiesAsync([phpsessidCookie]);

            await _page.ReloadAsync();
        }

        public async Task<UserInfo> GetUserInfoAsync()
        {
            var page = new SteamGiftPage(_page);
            await page.GoToPage(1);
            Thread.Sleep(DefaultWaitTime);
            var isAuthorized = await page.IsAuthorizedAsync();
            if (isAuthorized == false)
            {
                _logger?.LogWarning("User is not authorized on SteamGifts");
                throw new UnauthorizedAccessException("User is not authorized on SteamGifts");
            }

            var userInfo = new UserInfo
            {
                Username = await page.GetUserNameAsync(),
                Points = await page.GetPointsAsync(),
                Level = await page.GetLevelAsync()
            };

            _logger?.LogInformation("User info: {Username}, Level {Level}, Points {Points}",
                userInfo.Username, userInfo.Level, userInfo.Points);

            return userInfo;
        }

        public async Task<IEnumerable<Giveaway>> GetAllGiveawaysAsync()
        {
            var page = new SteamGiftPage(_page);
            var result = new List<Giveaway>();
            int currentPage = 1;

           
            do
            {
                _logger?.LogInformation("Loading giveaways from page {Page}", currentPage);
                await page.GoToPage(currentPage++);

                Thread.Sleep(DefaultWaitTime);

                var isConsentButtonVisible = await page.IsConsentButtonVisibleAsync();
                if (isConsentButtonVisible)
                {
                    _logger?.LogInformation("Consent button is visible, clicking it.");
                    await page.ClickConsentButtonIfExistsAsync();
                    Thread.Sleep(DefaultWaitTime);
                }

                var giveaways = await page.GetGiveawaysAsync();
                Thread.Sleep(DefaultWaitTime);
                var giveawaysTasks = giveaways.Select(async g => new Giveaway
                {
                    GameName = await g.GetGameNameAsync(),
                    GiveawayUrl = await g.GetGiveawayUrlAsync(),
                    SteamUrl = await g.GetSteamUrlAsync(),
                    Points = await g.GetPointsAsync(),
                    Level = await g.GetLevelAsync(),
                    ApplicationId = await g.GetApplicationIdAsync(),
                    Joined = await g.HasAlreadyJoinedAsync(),
                    IsCollection = await g.IsCollectionAsync()
                });
                var resolvedGiveaways = await Task.WhenAll(giveawaysTasks);
                result.AddRange(resolvedGiveaways);
            }
            while (await page.IsNextPageAvailableAsync());

            _logger?.LogInformation("Collected total {Count} giveaways", result.Count);
            return result;
        }

        public async Task<bool> JoinGiveawayAsync(string giveawayUrl)
        {
            var page = new GiveawayPage(_page, _baseUrl + giveawayUrl);

            _logger?.LogDebug("Trying to join giveaway: {Url}", giveawayUrl);

            await page.GoToPageAsync();
            Thread.Sleep(DefaultWaitTime);

            bool result = await page.PerformEnterAsync();

            _logger?.LogInformation("Joined giveaway {Url}: {Result}", giveawayUrl, result);

            return result;
        }
    }
}
