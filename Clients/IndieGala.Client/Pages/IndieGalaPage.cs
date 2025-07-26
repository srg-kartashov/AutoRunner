using IndieGala.Client.Models;
using IndieGala.Client.Pages.Elements;

using Microsoft.Playwright;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndieGala.Client.Pages
{
    internal class IndieGalaPage : BasePage
    {
        private readonly string baseUrl = "https://www.indiegala.com/giveaways";
        private string UserNameSelector => "li.avatar-username div.username-text";
        private string PointsSelector => "li.user-wallet span#galasilver-amount";
        private string LevelSelector => "li.user-wallet span#userGiveawaysLevel";
        private string LoginButtonSelector => "div.left username div.username-text";
        private string DisallowButtonSelector => "div.sp-prompt-message button.sp-prompt-btn.sp-disallow-btn";
        private string SpinButtonSelector => "div.fortune-wheel-cont.flex.relative div.fortune-wheel-outer.relative button";
        private string FortuneResultButtonCloseSelector => "div.fortune-wheel-results div.flex button";

        public string GiveawaysSelector => "div.page-contents-list div.items-list-row div.items-list-item";

        public IndieGalaPage(IPage page) : base(page)
        {
        }

        public async Task<bool> IsAuthorizedAsync()
        {
            try
            {
                var loginButtons = Page.Locator(LoginButtonSelector);
                return await loginButtons.CountAsync() == 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<string> GetUserNameAsync()
        {
            var element = await Page.QuerySelectorAsync(UserNameSelector);
            if (element == null)
                return string.Empty;

            var text = await element.TextContentAsync();
            return text ?? string.Empty;
        }

        public async Task<bool> PopupVisible()
        {
            try
            {
                var loginButtons = Page.Locator(DisallowButtonSelector);
                await loginButtons.First.WaitForAsync(new LocatorWaitForOptions
                {
                    Timeout = 10000, 
                    State = WaitForSelectorState.Visible
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task ClickDisallowButtonAsync()
        {
            var button = Page.Locator(DisallowButtonSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
            }
        }

        public async Task<bool> FortuneWheelVisible()
        {
            try
            {
                var loginButtons = Page.Locator(SpinButtonSelector);
                await loginButtons.First.WaitForAsync(new LocatorWaitForOptions
                {
                    Timeout = 10000,
                    State = WaitForSelectorState.Visible
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task ClickSpinButtonAsync()
        {
            var button = Page.Locator(SpinButtonSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
            }
        }

        public async Task ClickCloseButtonAsync()
        {
            var button = Page.Locator(FortuneResultButtonCloseSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
            }
        }

        public async Task<int> GetPointsAsync()
        {
            var element = await Page.QuerySelectorAsync(PointsSelector);
            if (element == null)
                return 0;


            var text = await element.TextContentAsync();
            return int.TryParse(text, out var points) ? points : 0;
        }

        public async Task<int> GetLevelAsync()
        {
            var element = await Page.QuerySelectorAsync(LevelSelector);
            if (element == null)
                return 0;

            var text = await element.TextContentAsync();
            return int.TryParse(text, out var points) ? points : 0;
        }

        public async Task GoToMainPage()
        {
            await Page.GotoAsync("https://www.indiegala.com/");
        }

        public async Task GoToPage()
        {
            await Page.GotoAsync(baseUrl);
            await Page.Locator(".page-contents-ajax-list-cover").WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden
            });
            await Page.Locator("section.page-contents-list-cont div.page-contents-list-menu").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible
            });
        }

        public async Task<bool> NextGiveawaysPageExistAsync()
        {
            await Page.Locator(".page-contents-ajax-list-cover").WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden
            });
            var paginationItems = await Page.QuerySelectorAllAsync("div.pagination a.prev-next i.fa.fa-angle-right");

            var item = paginationItems.FirstOrDefault();

            return item != null;
        }

        public async Task<bool> NavigateToNextPageAsync()
        {
            var paginationItems = await Page.QuerySelectorAllAsync("div.pagination a.prev-next i.fa.fa-angle-right");

            var button = paginationItems.FirstOrDefault();
            if (button == null)
                return false;

            await Page.Locator(".page-contents-ajax-list-cover").WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden
            });

            await button.WaitForElementStateAsync(ElementState.Visible);

            await button.ClickAsync();

            return true;
        }

        public async Task<IEnumerable<GiveawayElement>> GetGiveawaysAsync()
        {
            var elements = await Page.Locator(GiveawaysSelector).AllAsync();

            var result = new List<GiveawayElement>();
            foreach (var element in elements)
            {
                result.Add(new GiveawayElement(Page, element));
            }

            return result;
        }
    }
}
