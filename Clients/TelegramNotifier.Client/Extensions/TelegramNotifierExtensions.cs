using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Telegram.Bot;

using TelegramNotifier.Client.Models;

namespace TelegramNotifier.Client.Extensions
{
    public static class TelegramNotifierExtensions
    {
        public static IServiceCollection AddTelegramNotifier(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TelegramNotifierOptions>(configuration.GetSection("TelegramNotifier"));

            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<TelegramNotifierOptions>>().Value;
                return new TelegramBotClient(options.BotToken);
            });

            services.AddScoped(typeof(ITelegramNotifier<>), typeof(TelegramNotifier<>));

            return services;
        }
    }
}
