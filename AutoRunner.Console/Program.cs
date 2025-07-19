using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using SteamGifts.Client;



namespace AutoRunner.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var driver = new ChromeDriver();

            // Переход на сайт, чтобы домен соответствовал куке
            driver.Navigate().GoToUrl("https://www.steamgifts.com");

            // Добавление куки
            var coockies = driver.Manage().Cookies.AllCookies;
            driver.Manage().Cookies.AddCookie(new Cookie("PHPSESSID", "", "www.steamgifts.com", "/", null));
            driver.Navigate().Refresh();

            SteamGiftsClient steamGiftsClient = new SteamGiftsClient(driver);
            // Example usage of the SteamGiftsClient
            try
            {
                var userInfo = steamGiftsClient.GetUserInfo();
                var giveaways = steamGiftsClient.GetAllGiveaways();
                foreach (var giveaway in giveaways.Where(e => !e.Joined))
                {
                    steamGiftsClient.JoinGiveaway(giveaway.GiveawayUrl);
                    Thread.Sleep(1000); // Sleep to avoid rate limiting
                    Console.WriteLine($"Game: {giveaway.GameName}, Points: {giveaway.Points}, Level: {giveaway.Level}, Joined: {giveaway.Joined}");
                }
                ;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
