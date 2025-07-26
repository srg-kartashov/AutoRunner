using Microsoft.Playwright;

namespace IndieGala.Client.Pages.Elements
{
    internal class BaseElement
    {
        public IPage Page { get; }
        public ILocator Locator { get; }

        public BaseElement(IPage page, ILocator locator)
        {
            Page = page;
            Locator = locator;
        }
    }
}