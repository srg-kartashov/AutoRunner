using Microsoft.Playwright;

using SteamGifts.Client.Pages.Elements;

using System.Threading.Tasks;
using System.Web;

namespace SteamGifts.Client.Pages
{
    internal class SteamGiftPage : BasePage
    {
        private readonly string baseUrl = "https://www.steamgifts.com/";
        private string CurrentPageSelector => "div.pagination__navigation a.is-selected span";
        private string GiveawaysSelector =>"div:not([class]) div:not([class]) div.giveaway__row-inner-wrap";
        private string LevelSelector => "a[href^='/account'] span[title]";
        private string PaginationSelector => "div.pagination__navigation span";
        private string PointsSelector => "a[href^='/account'] span.nav__points";
        private string UserNameSelector => "header a[href^='/user']";
        private string ConsentButtonSelector => "div.fc-footer-buttons-container button[aria-label='Consent']";


        public SteamGiftPage(IPage page) : base(page)
        {
        }

        public async Task<int> GetCurrentPage()
        {
            var currentPageElement = await Page.QuerySelectorAsync(CurrentPageSelector);
            string? number = null;

            if (currentPageElement != null)
            {
                number = await currentPageElement.InnerTextAsync();
            }
            if (number == null)
            {
                if (Page.Url.Trim('/').EndsWith("steamgifts.com"))
                    return 1;
                var Uri = new Uri(Page.Url);
                number = HttpUtility.ParseQueryString(Uri.Query).Get("page");
            }
            if (int.TryParse(number, out var result))
                return result;
            throw new Exception("Не смогли определить номер текущей страницы");
        }

        public async Task<IEnumerable<GiveawayElement>> GetGiveawaysAsync()
        {
            var elements = await Page.Locator(GiveawaysSelector).AllAsync();

            var result = new List<GiveawayElement>();
            foreach (var element in elements)
            {
                result.Add(new GiveawayElement(Page, element));
            }

            return result;
        }

        public async Task<bool> IsGiveawaysAvailable()
        {
            var giveaways = await Page.QuerySelectorAllAsync(GiveawaysSelector);
            return giveaways != null && giveaways.Count > 0;
        }

        public async Task<int> GetLevelAsync()
        {
            var levelElements = await Page.QuerySelectorAllAsync(LevelSelector);
            var levelElement = levelElements.Last();
            if (levelElement != null)
            {
                var levelText = await levelElement.InnerTextAsync();
                var levelValue = levelText.Split(' ').LastOrDefault();
                if (levelValue != null && int.TryParse(levelValue, out var level))
                {
                    return level;
                }
            }
            return 0;
        }

        public async Task<int> GetPointsAsync()
        {
            var elements = await Page.QuerySelectorAllAsync(PointsSelector);
            var points = elements.FirstOrDefault();
            try
            {
                var text = await points.InnerTextAsync();
                var textPoints = text.Split(" ")?.LastOrDefault();
                return Convert.ToInt32(textPoints);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<string> GetUserNameAsync()
        {
            var elements = await Page.QuerySelectorAllAsync(UserNameSelector);
            var userNameElement = elements.FirstOrDefault();
            var href = await userNameElement?.GetAttributeAsync("href") ?? string.Empty;
            var username = href.Split('/').LastOrDefault() ?? string.Empty;
            return username;
        }

        public async Task GoToNextPage()
        {
            var Uri = new Uri(Page.Url);
            var queryPage = HttpUtility.ParseQueryString(Uri.Query).Get("page");
            if (queryPage != null)
            {
                var pageNumber = int.Parse(queryPage);
                await GoToPage(pageNumber + 1);
            }
            else
            {
                await GoToPage(2);
            }
        }

        public async Task GoToPage(int pageNumber)
        {
            string url = pageNumber == 1 ? baseUrl : $"{baseUrl}giveaways/search?page={pageNumber}";
            await Page.GotoAsync(url);
        }

        public async Task<bool> IsAuthorizedAsync()
        {
            try
            {
                var elements = Page.Locator(UserNameSelector);
                var userNameVisible = await elements.IsVisibleAsync();
                return userNameVisible;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> IsNextPageAvailableAsync()
        {
            var elements = await Page.QuerySelectorAllAsync(PaginationSelector);
            var pagination = elements.LastOrDefault();
            var nextPageExists = await pagination?.InnerTextAsync() == "Next";
            return nextPageExists;
        }

        public async Task<bool> IsConsentButtonVisibleAsync()
        {
            var button = Page.Locator(ConsentButtonSelector);
            return await button.IsVisibleAsync();
        }

        public async Task ClickConsentButtonIfExistsAsync()
        {
            var button = Page.Locator(ConsentButtonSelector);
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
                await Page.WaitForTimeoutAsync(1000); // как Thread.Sleep(1000)
            }
        }
    }
}