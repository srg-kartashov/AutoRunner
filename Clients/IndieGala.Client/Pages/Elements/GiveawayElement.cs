using Microsoft.Playwright;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IndieGala.Client.Pages.Elements
{
    internal class GiveawayElement : BaseElement
    {
        private string TitleSelector => "h5.items-list-item-title a";
        private string ImageSelector => "figure img";
        public string PointsSelector => "div.items-list-item-data a[data-price]";
        public string LevelSelector => "figcaption div:has-text(\"single ticket\") span";

        public GiveawayElement(IPage page, ILocator locator) : base(page, locator)
        {
        }

        public async Task<string> GetGameNameAsync()
        {
            var gameName = Locator.Locator(TitleSelector);
            return await gameName.InnerTextAsync();
        }

        public async Task<string> GetGiveawayUrlAsync()
        {
            var element = Locator.Locator(TitleSelector);
            return await element.First.GetAttributeAsync("href") ?? string.Empty;
        }

        public async Task<string> GetApplicationIdAsync()
        {
            try
            {
                var element = Locator.Locator(ImageSelector);
                var element1 = Locator.Locator(ImageSelector).CountAsync();
                var href = await element.First.GetAttributeAsync("data-img-src");
                return href != null ? ParseAppId(href) : string.Empty;
            }
            catch(Exception ex)
            {
                ;
            }
            return string.Empty;
        }

        private string ParseAppId(string url)
        {
            string gameUrlPattern = @"\/steam\/apps\/(\d+)\/";
            string collectionUrlPattern = @"\/steam\/sub\/(\d+)\/";
            MatchCollection matches;
            if (Regex.IsMatch(url, gameUrlPattern))
            {
                matches = Regex.Matches(url, gameUrlPattern);
            }
            else if (Regex.IsMatch(url, collectionUrlPattern))
            {
                matches = Regex.Matches(url, collectionUrlPattern);
            }
            else
                throw new InvalidDataException();


            var result = matches.First().Groups.Values.Last().Value;
            return result;
        }

        public async Task<int> GetPointsAsync()
        {
            var element = Locator.Locator(PointsSelector);
            if (await element.CountAsync() > 0)
            {
                var dataPrice = await element.First.GetAttributeAsync("data-price");

                return int.TryParse(dataPrice, out var points) ? points : 0;
            }
            return 0;          
        }

        public async Task<int> GetLevelAsync()
        {
            try
            {
                var element = Locator.Locator(LevelSelector);
                var text = await element.InnerTextAsync(new() { Timeout = 1000 });
                var level = text.Split(" ").LastOrDefault();

                if (!string.IsNullOrEmpty(level))
                {
                    if (int.TryParse(level, out var result))
                    {
                        return result;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task<bool> HasAlreadyJoinedAsync()
        {
            var locator = Locator.Locator(PointsSelector);

            bool exists = await locator.CountAsync() == 0;

            return exists;
        }
    }
}
