using Microsoft.Playwright;

namespace AutoRunner.Factories
{
    public interface IPlaywrightDriverFactory
    {
        Task<IPage> CreatePageAsync();
    }
}