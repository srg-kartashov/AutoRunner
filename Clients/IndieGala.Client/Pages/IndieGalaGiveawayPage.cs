using Microsoft.Playwright;

namespace IndieGala.Client.Pages
{
    internal class IndieGalaGiveawayPage : BasePage
    {
        private string JoinButtonSelector => "div.card-contents div.card-ticket div.card-join a";

        public string Url { get; }

        public IndieGalaGiveawayPage(IPage page, string url) : base(page)
        {
            Url = url;
        }

        public async Task GoToPageAsync()
        {
            await Page.GotoAsync(Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            await Page.Locator(JoinButtonSelector).WaitForAsync(new() { Timeout = 30000 });
        }

        public async Task<bool> PerformJoinAsync()
        {
            await Task.Delay(3000);
            await ClickJoinButtonAsync();
            await Task.Delay(3000);
            return await IsJoinedAsync();
        }

        private async Task<bool> IsJoinedAsync()
        {
            var button = Page.Locator(JoinButtonSelector);

            try
            {
                await button.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Detached, 
                    Timeout = 10_000
                });

                return true;
            }
            catch (TimeoutException)
            {
                try
                {
                    var joinedButton = Page.Locator(JoinButtonSelector);
                    var text = await joinedButton.TextContentAsync();
                    return text != null && text.Contains("Joined", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task ClickJoinButtonAsync()
        {
            var button = Page.Locator(JoinButtonSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
            }
        }
    }
}
