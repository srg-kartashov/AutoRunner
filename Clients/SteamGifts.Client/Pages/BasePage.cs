using Microsoft.Playwright;

using SteamGifts.Client.Utils;

namespace SteamGifts.Client.Pages
{
    internal class BasePage
    {
        protected IPage Page { get; }
        protected RandomWaiter RandomWaiter { get; }

        public BasePage(IPage page)
        {
            Page = page;
            RandomWaiter = new RandomWaiter();
        }

        public async Task RefreshPageAsync()
        {
            await Page.ReloadAsync();
        }
    }
}