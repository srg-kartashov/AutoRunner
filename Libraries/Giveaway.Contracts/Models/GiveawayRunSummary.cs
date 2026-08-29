namespace Giveaway.Contracts.Models;

public sealed class GiveawayRunSummary
{
    public GiveawayUser User { get; init; } = new();
    public int TotalGiveaways { get; set; }
    public int Joined { get; set; }
    public int Hidden { get; set; }
    public int AlreadyJoined { get; set; }
    public int SkippedCollections { get; set; }
    public int SkippedByLevel { get; set; }
    public int MissingReviewData { get; set; }
    public int SkippedByFilter { get; set; }
    public int InsufficientPoints { get; set; }
    public int FailedJoins { get; set; }
    public int FailedHides { get; set; }
    public int RemainingPoints { get; set; }

    public string ToDisplayText() => string.Join(Environment.NewLine,
    [
        "SteamGifts giveaway summary",
        $"User: {User.Username} (level {User.Level})",
        $"Total giveaways: {TotalGiveaways}",
        $"Joined: {Joined}",
        $"Hidden: {Hidden}",
        $"Already joined: {AlreadyJoined}",
        $"Skipped collections: {SkippedCollections}",
        $"Skipped by level: {SkippedByLevel}",
        $"Missing review data: {MissingReviewData}",
        $"Skipped by filter: {SkippedByFilter}",
        $"Insufficient points: {InsufficientPoints}",
        $"Failed joins: {FailedJoins}",
        $"Failed hides: {FailedHides}",
        $"Remaining points: {RemainingPoints}"
    ]);
}
