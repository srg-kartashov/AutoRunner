namespace Giveaway.Contracts.Options;

public sealed class SteamGiftsOptions
{
    public const string SectionName = "SteamGifts";

    public string Token { get; set; } = string.Empty;
    public int ActionDelaySeconds { get; set; } = 2;
    public SteamGiftsBrowserOptions Browser { get; set; } = new();
    public GiveawayFilterOptions Filters { get; set; } = new();
}

public sealed class SteamGiftsBrowserOptions
{
    public bool Headless { get; set; } = true;
    public string UserDataDirectory { get; set; } = "playwright-user-data";
}

public sealed class GiveawayFilterOptions
{
    public bool EnterCollections { get; set; }
    public bool StopWhenOutOfPoints { get; set; }
    public List<RatingReviewRange> Join { get; set; } = [];
    public List<RatingReviewRange> Hide { get; set; } = [];
}

public sealed class RatingReviewRange
{
    public int RatingFrom { get; set; }
    public int RatingTo { get; set; } = 100;
    public int ReviewsFrom { get; set; }
    public int ReviewsTo { get; set; } = int.MaxValue;

    public bool Matches(double rating, int totalReviews) =>
        rating >= RatingFrom &&
        rating <= RatingTo &&
        totalReviews >= ReviewsFrom &&
        totalReviews <= ReviewsTo;
}
