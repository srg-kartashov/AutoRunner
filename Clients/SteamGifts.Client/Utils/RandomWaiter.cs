namespace SteamGifts.Client.Utils
{
    internal class RandomWaiter
    {
        private Random Random { get; }

        public RandomWaiter()
        {
            Random = new Random();
        }

        public async Task WaitHours(int hoursFrom, int hoursTo) => await WaitMinutes(hoursFrom * 60, hoursTo * 60);

        public async Task WaitHours(int hours) => await WaitHours(0, hours);

        public async Task WaitMinutes(int minutesFrom, int minutesTo) => await WaitSeconds(minutesFrom * 60, minutesTo * 60);

        public async Task WaitMinutes(int minutes) => await WaitMinutes(0, minutes);

        public async Task WaitSeconds(int secondsFrom, int secondsTo) => await WaitMilliseconds(secondsFrom * 1000, secondsTo * 1000);

        public async Task WaitSeconds(int seconds) => await WaitSeconds(0, seconds);

        private async Task WaitMilliseconds(int from, int to)
        {
            var value = Random.Next(from, to);
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(value);
            await Task.Delay(value);
        }
    }
}