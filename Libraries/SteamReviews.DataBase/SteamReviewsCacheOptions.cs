namespace SteamReviews.DataBase;

public sealed class SteamReviewsCacheOptions
{
    public const string SectionName = "SteamPowered:ReviewsCache";

    public string DatabasePath { get; set; } = "data/steam-reviews-cache.db";

    public int ExpirationDays { get; set; } = 7;
}
