namespace TelegramNotifier.Client.Models
{
    public class TelegramNotifierOptions
    {
        public string BotToken { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string DefaultTopic { get; set; } = string.Empty;
        public Dictionary<string, int> TopicMap { get; set; } = new();
    }
}
