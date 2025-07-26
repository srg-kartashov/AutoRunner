namespace SteamGifts.Client.Models
{
    public class SteamGiftsGiveaway
    {
        public string GameName { get; set; } = string.Empty;
        public string GiveawayUrl { get; set; } = string.Empty;
        public string SteamUrl { get; set; } = string.Empty;
        public int Points { get; set; }
        public int Level { get; set; }
        public string ApplicationId { get; set; } = string.Empty;
        public bool Joined { get; set; }
        public bool IsCollection { get; internal set; }
    }
}
