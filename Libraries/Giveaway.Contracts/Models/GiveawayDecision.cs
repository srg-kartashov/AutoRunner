namespace Giveaway.Contracts.Models;

public sealed record GiveawayDecision(
    GiveawayCandidate Giveaway,
    GiveawayDecisionAction Action,
    GiveawayDecisionReason Reason,
    AppReview? Review = null)
{
    public double Score => Review is null
        ? 0
        : Review.Rating * Math.Log10(Review.TotalReviews + 1);
}

public enum GiveawayDecisionAction
{
    Join,
    Hide,
    Skip
}

public enum GiveawayDecisionReason
{
    JoinFilterMatched,
    CollectionAllowed,
    HideFilterMatched,
    AlreadyJoined,
    RequiredLevelNotMet,
    CollectionDisabled,
    MissingApplicationId,
    MissingReviewData,
    NoMatchingFilter,
    InsufficientPoints,
    JoinFailed,
    HideFailed
}
