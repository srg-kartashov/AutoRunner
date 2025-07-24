using Microsoft.Playwright;

namespace SteamGifts.Client.Pages
{
    internal class GiveawayPage : BasePage
    {
        public string Url { get; }
        private string ConfirmHideButtonSelector => "div.form__submit-button";
        private string DeleteButtonSelector => "form div[data-do='entry_delete'].sidebar__entry-delete";
        private string EnterButtonSelector => "form div[data-do='entry_insert'].sidebar__entry-insert";
        private string HideButtonSelector => "div.featured__heading i.featured__giveaway__hide";

        public GiveawayPage(IPage page, string url) : base(page)
        {
            Url = url;
        }

        public async Task GoToPageAsync()
        {
            await Page.GotoAsync(Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            await Task.WhenAny(
                Page.Locator(HideButtonSelector).WaitForAsync(new() { Timeout = 30000 }),
                Page.Locator(EnterButtonSelector).WaitForAsync(new() { Timeout = 30000 })
            );
        }

        public async Task<bool> PerformEnterAsync()
        {
            await RandomWaiter.WaitSeconds(1, 3);
            await ClickEnterButtonAsync();
             
            await RandomWaiter.WaitSeconds(1, 3);
            return await IsEnteredAsync();
        }

        public async Task<bool> PerformHideAsync()
        {
          
            if (await IsHiddenAsync())
                return true;
            await ClickHideButtonAsync();
            await RandomWaiter.WaitSeconds(1, 3);
            await ClickConfirmButtonAsync();
            await RandomWaiter.WaitSeconds(1, 3);
            return await IsHiddenAsync();
        }

        private async Task ClickConfirmButtonAsync()
        {
            var button = Page.Locator(ConfirmHideButtonSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
            }
        }

        private async Task ClickEnterButtonAsync()
        {
            var button = Page.Locator(EnterButtonSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
            }
        }

        private async Task ClickHideButtonAsync()
        {
            var button = Page.Locator(HideButtonSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
            }
        }

        private async Task<bool> IsEnterButtonVisibleAsync()
        {
            return await Page.Locator(EnterButtonSelector).IsVisibleAsync();
        }

        private async Task<bool> IsHideButtonVisibleAsync()
        {
            return await Page.Locator(HideButtonSelector).IsVisibleAsync();
        }

        private async Task<bool> IsConfirmButtonVisibleAsync()
        {
            return await Page.Locator(ConfirmHideButtonSelector).IsVisibleAsync();
        }

        private async Task<bool> IsEnteredAsync()
        {
            try
            {
                var enterButton = Page.Locator(DeleteButtonSelector);
                var classValue = await enterButton.GetAttributeAsync("class");
                return classValue is not string cls || !cls.Contains("is-hidden");
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> IsHiddenAsync()
        {
            return await Page.Locator(HideButtonSelector).IsVisibleAsync();
        }
    }
}