using OpenQA.Selenium;

namespace AutoRunner.Factories
{
    public interface ISeleniumDriverFactory
    {
        IWebDriver CreateDriver();
    }
}