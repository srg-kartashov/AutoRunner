using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using TelegramNotifier.Client.Models;

namespace TelegramNotifier.Client
{
    public class TelegramNotifier<T> : ITelegramNotifier<T>
    {
        private readonly ITelegramBotClient _bot;
        private readonly string _groupId;
        private readonly int? _messageThreadId;
        private readonly ILogger<T> _logger;

        public TelegramNotifier(
            ITelegramBotClient bot,
            IOptions<TelegramNotifierOptions> configOptions,
            ILogger<T> logger)
        {
            _bot = bot;
            _groupId = configOptions.Value.GroupId;
            _messageThreadId = configOptions.Value.TopicMap?.TryGetValue(typeof(T).Name, out var id) == true ? id : null;
            _logger = logger;
        }

        public async Task SendTextAsync(string message, bool disableNotification = false)
        {
            await _bot.SendMessage(
                chatId: _groupId,
                text: message,
                messageThreadId: _messageThreadId,
                disableNotification: disableNotification,
                parseMode: ParseMode.Markdown);
        }

        public async Task SendFileAsync(Stream fileStream, string fileName, string caption = "")
        {
            var inputFile = new InputFileStream(fileStream, fileName);
            await _bot.SendDocument(
                chatId: _groupId,
                document: inputFile,
                caption: caption,
                messageThreadId: _messageThreadId);
        }

        public async Task SendScreenshotAsync(byte[] imageBytes, string caption = "")
        {
            using var ms = new MemoryStream(imageBytes);
            var inputFile = new InputFileStream(ms, "screenshot.png");
            await _bot.SendPhoto(
                chatId: _groupId,
                photo: inputFile,
                caption: caption,
                messageThreadId: _messageThreadId);
        }
    }
}
