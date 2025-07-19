using OpenQA.Selenium;

using SteamGifts.Client.Utils;

namespace SteamGifts.Client.Pages
{
    internal class BasePage
    {
        protected IWebDriver Driver { get; }
        protected RandomWaiter RandomWaiter { get; }

        public BasePage(IWebDriver driver)
        {
            Driver = driver;
            RandomWaiter = new RandomWaiter();
        }

        public void RefreshPage()
        {
            Driver.Navigate().Refresh();
        }
    }
}