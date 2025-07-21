
namespace TelegramNotifier.Client
{
    public interface ITelegramNotifier<T>
    {
        Task SendFileAsync(Stream fileStream, string fileName, string caption = "");
        Task SendScreenshotAsync(byte[] imageBytes, string caption = "");
        Task SendTextAsync(string message, bool disableNotification = false);
    }
}