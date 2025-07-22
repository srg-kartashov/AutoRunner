using Microsoft.Playwright;

namespace AutoRunner.Factories
{
    public class PlaywrightDriverFactory : IPlaywrightDriverFactory
    {
        public async Task<IPage> CreatePageAsync()
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                "--window-size=1920,1080",
                "--no-sandbox",
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-extensions",
                "--disable-software-rasterizer",
                "--no-zygote"
                }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36"
            });

            var page = await context.NewPageAsync();
            return page;
        }
    }
}
