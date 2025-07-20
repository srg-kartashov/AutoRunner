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
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");

            return new ChromeDriver(options);
        }
    }
}
