using IndieGala.Client.Models;
using IndieGala.Client.Pages;
using IndieGala.Client.Pages.Elements;

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndieGala.Client
{
    public class IndieGalaClient
    {
        private readonly IPage _page;
        private readonly ILogger? _logger;
        private const int DefaultWaitTime = 1000; // Default wait time in milliseconds
        private string _baseUrl = "https://www.indiegala.com";

        public IndieGalaClient(IPage page, ILogger? logger = null)
        {
            _page = page;
            _logger = logger;
        }

        public async Task AuthAsync(string sessionId)
        {
            var page = new IndieGalaPage(_page);
            await page.GoToPage();
            await Task.Delay(DefaultWaitTime * 5);
            var isAuthorized = await page.IsAuthorizedAsync();
            if(!isAuthorized)
            {
                _logger?.LogInformation("User is not authorized, attempting to authenticate with session ID.");
                var context = _page.Context;
                await context.ClearCookiesAsync();

                await context.AddCookiesAsync(
                    [
                        new Cookie { Name = "sessionid", Value = sessionId, Domain = ".indiegala.com", Path = "/" },
                ]);

                await _page.ReloadAsync();
            }
            else
            {
                _logger?.LogInformation("User is already authorized on IndieGala");
            }
        }

        public async Task<UserInfo> GetUserInfoAsync()
        {
            var page = new IndieGalaPage(_page);
            await page.GoToMainPage();
            await Task.Delay(DefaultWaitTime);
            var isAuthorized = await page.IsAuthorizedAsync();
            if (isAuthorized == false)
            {
                _logger?.LogWarning("User is not authorized on SteamGifts");
                throw new UnauthorizedAccessException("User is not authorized on SteamGifts");
            }
            await Task.Delay(DefaultWaitTime);
            if (await page.PopupVisible())
            {
                await Task.Delay(DefaultWaitTime);
                _logger?.LogWarning("Disallow button is visible, clicking it to close the popup");
                await page.ClickDisallowButtonAsync();
            }
            await Task.Delay(DefaultWaitTime);
            if (await page.FortuneWheelVisible())
            {
                await page.ClickSpinButtonAsync();
            }
            await Task.Delay(DefaultWaitTime);
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
            var page = new IndieGalaPage(_page);
            var result = new List<Giveaway>();
            bool firstPage = true;
            int currentPage = 1;
             
            await page.GoToPage();

            await Task.Delay(DefaultWaitTime);
            if (await page.PopupVisible())
            {
                _logger?.LogWarning("Disallow button is visible, clicking it to close the popup");
                await page.ClickDisallowButtonAsync();
            }
            do
            {
                await Task.Delay(DefaultWaitTime);
                _logger?.LogInformation("Loading giveaways from page {Page}", currentPage++);

                if (!firstPage)
                {
                    if (await page.NextGiveawaysPageExistAsync())
                    {
                        await page.NavigateToNextPageAsync();
                    }
                    else
                    {
                        _logger?.LogInformation("No more giveaways pages available.");
                        break;
                    }
                }

                await Task.Delay(DefaultWaitTime * 3);

                var giveaways = await page.GetGiveawaysAsync();
               
                var giveawaysTasks = giveaways.Select(async g => new Giveaway
                {
                    GameName = await g.GetGameNameAsync(),
                    GiveawayUrl = await g.GetGiveawayUrlAsync(),
                    Points = await g.GetPointsAsync(),
                    Level = await g.GetLevelAsync(),
                    ApplicationId = await g.GetApplicationIdAsync(),
                    Joined = await g.HasAlreadyJoinedAsync()
                });
                var resolvedGiveaways = await Task.WhenAll(giveawaysTasks);
                result.AddRange(resolvedGiveaways);

                firstPage = false;
            }
            while (await page.NextGiveawaysPageExistAsync());

            _logger?.LogInformation("Collected total {Count} giveaways", result.Count);
            return result;
        }

        public async Task<bool> JoinGiveawayAsync(string giveawayUrl)
        {
            var page = new IndieGalaGiveawayPage(_page, _baseUrl + giveawayUrl);

            _logger?.LogDebug("Trying to join giveaway: {Url}", giveawayUrl);

            await page.GoToPageAsync();
            await Task.Delay(DefaultWaitTime);

            bool result = await page.PerformJoinAsync();

            _logger?.LogInformation("Joined giveaway {Url}: {Result}", giveawayUrl, result);

            return result;
        }
    }
}
