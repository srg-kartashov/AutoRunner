using Microsoft.Playwright;

namespace AutoRunner.Factories
{
    public record PlaywrightContext(
     IPlaywright Playwright,
     IBrowserContext BrowserContext,
     IPage Page) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Page.CloseAsync();
            await BrowserContext.CloseAsync();
            Playwright.Dispose();
        }
    }
}
