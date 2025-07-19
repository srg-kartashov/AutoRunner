namespace SteamGifts.Client.Models
{
    public class Giveaway
    {
        public required string GameName { get; set; }
        public required string GiveawayUrl { get; set; }
        public required string SteamUrl { get; set; }
        public required int Points { get; set; }
        public required int Level { get; set; }
        public required string ApplicationId { get; set; }
        public required bool Joined { get; set; }
    }
}
