using OpenQA.Selenium;

using System.Net.Http.Headers;

namespace SteamGifts.Client
{
    public static class ScreenshotSender
    {
        public static async Task CaptureAndSendScreenshotAsync(IWebDriver driver, string botToken, string chatId)
        {
            try
            {
                if (driver is not ITakesScreenshot screenshotDriver)
                    throw new InvalidOperationException("Driver does not support taking screenshots.");

                var screenshot = screenshotDriver.GetScreenshot();
                var filePath = Path.Combine(Path.GetTempPath(), $"screenshot_{Guid.NewGuid():N}.png");
                screenshot.SaveAsFile(filePath);

                await using var fileStream = File.OpenRead(filePath);
                using var form = new MultipartFormDataContent
            {
                { new StringContent(chatId), "chat_id" },
                { new StreamContent(fileStream)
                    {
                        Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
                    }, "photo", Path.GetFileName(filePath) }
            };

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync($"https://api.telegram.org/bot{botToken}/sendPhoto", form);
                response.EnsureSuccessStatusCode();

                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScreenshotSender] Error: {ex.Message}");
            }
        }
    }
}
