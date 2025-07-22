using Microsoft.Playwright;

namespace AutoRunner.Factories
{
    public interface IPlaywrightDriverFactory
    {
        Task<PlaywrightContext> CreateContextAsync();
    }
}