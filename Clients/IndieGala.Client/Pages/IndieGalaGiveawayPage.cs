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

            var detachedTask = button.WaitForAsync(new()
            {
                State = WaitForSelectorState.Detached,
                Timeout = 10_000
            }).ContinueWith(t => !t.IsFaulted);

            var joinedTextTask = Task.Run(async () =>
            {
                try
                {
                    await button.WaitForAsync(new()
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

                    var text = await button.TextContentAsync();
                    return text != null && text.Contains("Joined", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });

            var results = await Task.WhenAll(detachedTask, joinedTextTask);
            return results.Any(r => r);
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
