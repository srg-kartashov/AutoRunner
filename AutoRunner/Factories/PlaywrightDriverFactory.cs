using Microsoft.Playwright;

namespace AutoRunner.Factories
{
    public class PlaywrightDriverFactory : IPlaywrightDriverFactory
    {
        public async Task<PlaywrightContext> CreateContextAsync()
        {
            var playwright = await Playwright.CreateAsync();
            var userDataDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-user-data");
            var context = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
            {
#if DEBUG
                Headless = false,
#else
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
                },
#endif

                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36"
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();


            return new PlaywrightContext(
                 playwright,
                 context,
                 page);
        }
    }
}

