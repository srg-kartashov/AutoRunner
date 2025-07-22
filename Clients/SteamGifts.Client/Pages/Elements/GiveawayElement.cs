using Microsoft.Playwright;

using System.Text.RegularExpressions;

namespace SteamGifts.Client.Pages.Elements
{
    internal class GiveawayElement : BaseElement
    {
        private string GameNameSelector => "a.giveaway__heading__name";
        private string GiveawayUrlSelector => "a.giveaway__heading__name";
        private string SteamUrlSelector => "a.giveaway__icon";
        private string PointsSelector => "span.giveaway__heading__thin";
        private string LevelSelector => "div.giveaway__columns div.giveaway__column--contributor-level";

        public GiveawayElement(IPage page, ILocator locator) : base(page, locator)
        {
        }

        public async Task<string> GetGameNameAsync()
        {
            var gameName = Locator.Locator(GameNameSelector);
            return await gameName.InnerTextAsync();
        }

        public async Task<string> GetSteamUrlAsync()
        {
            var steamUrl = Locator.Locator(SteamUrlSelector);
            return await steamUrl.First.GetAttributeAsync("href") ?? string.Empty;
        }

        public async Task<string> GetGiveawayUrlAsync()
        {
            var giveawayUrl = Locator.Locator(GiveawayUrlSelector);
            return await giveawayUrl.First.GetAttributeAsync("href") ?? string.Empty;
        }

        public async Task<bool> HasAlreadyJoinedAsync()
        {
            var classAttribute = await Locator.GetAttributeAsync("class");
            return classAttribute?.Contains("is-faded") ?? false;
        }

        public async Task<bool> IsCollectionAsync()
        {
            var giveawayUrl = await GetSteamUrlAsync();
            bool isCollection = giveawayUrl.Contains("sub");
            return isCollection;
        }

        public async Task<int> GetPointsAsync()
        {
            try
            {
                var elements = await Locator.Locator(PointsSelector).AllAsync();
                foreach (var el in elements)
                {
                    var text = await el.InnerTextAsync();
                    if (text.EndsWith("P)"))
                    {
                        var pointText = text.Trim('(', ')').TrimEnd('P');
                        return int.Parse(pointText);
                    }
                }

                return int.MaxValue; // не нашли подходящий элемент
            }
            catch
            {
                return int.MaxValue; // ошибка при парсинге
            }
        }

        public async Task<string> GetApplicationIdAsync()
        {
            var steamUrl = await GetSteamUrlAsync();
            string[] patterns = [
                @"store.steampowered.com\/app\/(\d+)\/",
                @"store.steampowered.com\/app\/(\d+)?",
                @"store.steampowered.com\/sub\/(\d+)\/",
                @"store.steampowered.com\/sub\/(\d+)?",
            ];
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(steamUrl, pattern))
                {
                    var applicationId = Regex.Match(steamUrl, pattern).Groups[1].Value;
                    return applicationId;
                }
            }
            throw new FormatException("Error parsing ApplicationId. Invalid string format.");
        }

        public async Task<int> GetLevelAsync()
        {
            var levelElements = await Locator.Locator(LevelSelector).AllAsync();

            foreach (var el in levelElements)
            {
                var levelText = await el.InnerTextAsync();
                if (!string.IsNullOrWhiteSpace(levelText))
                {
                    var match = Regex.Match(levelText, @"\d+");
                    if (match.Success)
                    {
                        return int.Parse(match.Value);
                    }
                    else
                    {
                        throw new FormatException($"Error parsing Level. Invalid string format: {levelText}");
                    }
                }
            }

            return 0;
        }
    }
}