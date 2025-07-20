using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AutoRunner.Factories
{
    public class SeleniumDriverFactory : ISeleniumDriverFactory
    {
        public IWebDriver CreateDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");

            return new ChromeDriver(options);
        }
    }
}
