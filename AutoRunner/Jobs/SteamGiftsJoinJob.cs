using Hangfire;
using Hangfire.MissionControl;
using Hangfire.RecurringJobExtensions;

namespace AutoRunner.Jobs
{
    [MissionLauncher(CategoryName = "SteamGift")]
    public class SteamGiftsJoinJob
    {
        private readonly ILogger<SteamGiftsJoinJob> _logger;

        public SteamGiftsJoinJob(ILogger<SteamGiftsJoinJob> logger)
        {
            _logger = logger;
        }

        [Mission(Name = "Join SteamGifts Giveaways", Description = "Automatically joins available SteamGifts giveaways")]
        [RecurringJob("*/5 * * * *")]
        [JobDisplayName("SteamGifts: Auto Join Giveaways")]
        public async Task JoinGiveaways()
        {
            _logger.LogInformation("🚀 Выполняем авто-вступление в раздачи SteamGifts");
        }
    }
}
