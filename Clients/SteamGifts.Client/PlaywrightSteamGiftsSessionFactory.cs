using Giveaway.Contracts.Options;
using Giveaway.Contracts.Ports;
using ManagedCode.Playwright.Stealth;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace SteamGifts.Client;

public sealed class PlaywrightSteamGiftsSessionFactory : ISteamGiftsSessionFactory
{
    private readonly ILogger<PlaywrightSteamGiftsSessionFactory> _logger;

    public PlaywrightSteamGiftsSessionFactory(ILogger<PlaywrightSteamGiftsSessionFactory> logger)
    {
        _logger = logger;
    }

    public async Task<ISteamGiftsSession> CreateAsync(
        SteamGiftsBrowserOptions browserOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(browserOptions);
        cancellationToken.ThrowIfCancellationRequested();

        var playwright = await Playwright.CreateAsync();
        try
        {
            var userDataDirectory = Path.GetFullPath(browserOptions.UserDataDirectory);
            var context = await playwright.Chromium.LaunchPersistentContextAsync(
                userDataDirectory,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = browserOptions.Headless,
                    Args = browserOptions.Headless
                        ?
                        [
                            "--window-size=1920,1080",
                            "--no-sandbox",
                            "--disable-gpu",
                            "--disable-dev-shm-usage",
                            "--disable-extensions",
                            "--disable-software-rasterizer",
                            "--no-zygote"
                        ]
                        : [],
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36"
                });
            await context.ApplyStealthAsync();
            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
            return new PlaywrightSteamGiftsSession(playwright, context, page, _logger);
        }
        catch
        {
            playwright.Dispose();
            throw;
        }
    }

}
