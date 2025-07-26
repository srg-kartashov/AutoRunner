using Microsoft.Playwright;

namespace IndieGala.Client.Pages
{
    internal class BasePage
    {
        protected IPage Page { get; }

        public BasePage(IPage page)
        {
            Page = page;
        }

        public async Task RefreshPageAsync()
        {
            await Page.ReloadAsync();
        }
    }
}