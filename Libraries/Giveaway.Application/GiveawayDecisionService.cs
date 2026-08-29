using Giveaway.Contracts.Models;
using Giveaway.Contracts.Options;

namespace Giveaway.Application;

public sealed class GiveawayDecisionService
{
    public GiveawayDecision Decide(
        GiveawayCandidate giveaway,
        GiveawayUser user,
        AppReview? review,
        GiveawayFilterOptions filters)
    {
        ArgumentNullException.ThrowIfNull(giveaway);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(filters);

        if (giveaway.IsAlreadyJoined)
            return new GiveawayDecision(giveaway, GiveawayDecisionAction.Skip, GiveawayDecisionReason.AlreadyJoined);

        if (giveaway.RequiredLevel > user.Level)
            return new GiveawayDecision(giveaway, GiveawayDecisionAction.Skip, GiveawayDecisionReason.RequiredLevelNotMet);

        if (giveaway.IsCollection)
        {
            return filters.EnterCollections
                ? new GiveawayDecision(giveaway, GiveawayDecisionAction.Join, GiveawayDecisionReason.CollectionAllowed)
                : new GiveawayDecision(giveaway, GiveawayDecisionAction.Skip, GiveawayDecisionReason.CollectionDisabled);
        }

        if (string.IsNullOrWhiteSpace(giveaway.ApplicationId))
            return new GiveawayDecision(giveaway, GiveawayDecisionAction.Skip, GiveawayDecisionReason.MissingApplicationId);

        if (review is null)
            return new GiveawayDecision(giveaway, GiveawayDecisionAction.Skip, GiveawayDecisionReason.MissingReviewData);

        if (filters.Join.Any(range => range.Matches(review.Rating, review.TotalReviews)))
            return new GiveawayDecision(giveaway, GiveawayDecisionAction.Join, GiveawayDecisionReason.JoinFilterMatched, review);

        if (filters.Hide.Any(range => range.Matches(review.Rating, review.TotalReviews)))
            return new GiveawayDecision(giveaway, GiveawayDecisionAction.Hide, GiveawayDecisionReason.HideFilterMatched, review);

        return new GiveawayDecision(giveaway, GiveawayDecisionAction.Skip, GiveawayDecisionReason.NoMatchingFilter, review);
    }
}
