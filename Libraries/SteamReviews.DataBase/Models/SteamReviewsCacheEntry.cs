namespace SteamReviews.DataBase.Models;

internal sealed class SteamReviewsCacheEntry
{
    public required string ApplicationId { get; init; }

    public int TotalReviews { get; set; }

    public double Rating { get; set; }

    public long ExpiresAtUnixSeconds { get; set; }
}
