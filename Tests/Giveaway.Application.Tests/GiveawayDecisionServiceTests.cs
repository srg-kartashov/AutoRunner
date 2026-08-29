using Giveaway.Application;
using Giveaway.Contracts.Models;
using Giveaway.Contracts.Options;

using Xunit;

namespace Giveaway.Application.Tests;

public sealed class GiveawayDecisionServiceTests
{
    private readonly GiveawayDecisionService _service = new();

    [Fact]
    public void Decide_joins_game_that_matches_a_join_range()
    {
        var decision = _service.Decide(
            Game(),
            User(),
            Review(rating: 75, reviews: 1_000),
            Filters(join: [Range(70, 100, 1_000, int.MaxValue)]));

        Assert.Equal(GiveawayDecisionAction.Join, decision.Action);
        Assert.Equal(GiveawayDecisionReason.JoinFilterMatched, decision.Reason);
    }

    [Fact]
    public void Decide_hides_game_that_matches_only_a_hide_range()
    {
        var decision = _service.Decide(
            Game(),
            User(),
            Review(rating: 85, reviews: 200),
            Filters(
                join: [Range(70, 100, 1_000, int.MaxValue)],
                hide: [Range(0, 100, 0, 499)]));

        Assert.Equal(GiveawayDecisionAction.Hide, decision.Action);
        Assert.Equal(GiveawayDecisionReason.HideFilterMatched, decision.Reason);
    }

    [Fact]
    public void Decide_joins_collection_when_enabled_without_review_lookup()
    {
        var decision = _service.Decide(
            Game(isCollection: true),
            User(),
            review: null,
            Filters(enterCollections: true));

        Assert.Equal(GiveawayDecisionAction.Join, decision.Action);
        Assert.Equal(GiveawayDecisionReason.CollectionAllowed, decision.Reason);
    }

    [Fact]
    public void Decide_skips_collection_when_disabled()
    {
        var decision = _service.Decide(
            Game(isCollection: true),
            User(),
            review: null,
            Filters(enterCollections: false));

        Assert.Equal(GiveawayDecisionAction.Skip, decision.Action);
        Assert.Equal(GiveawayDecisionReason.CollectionDisabled, decision.Reason);
    }

    [Fact]
    public void Decide_skips_already_joined_game_before_applying_filters()
    {
        var decision = _service.Decide(
            Game(isAlreadyJoined: true),
            User(),
            Review(rating: 75, reviews: 1_000),
            Filters(join: [Range(70, 100, 1_000, int.MaxValue)]));

        Assert.Equal(GiveawayDecisionAction.Skip, decision.Action);
        Assert.Equal(GiveawayDecisionReason.AlreadyJoined, decision.Reason);
    }

    [Fact]
    public void Decide_uses_inclusive_range_boundaries()
    {
        var decision = _service.Decide(
            Game(),
            User(),
            Review(rating: 70, reviews: 1_000),
            Filters(join: [Range(70, 100, 1_000, int.MaxValue)]));

        Assert.Equal(GiveawayDecisionAction.Join, decision.Action);
    }

    [Fact]
    public void Validate_rejects_missing_steamgifts_token()
    {
        var result = new SteamGiftsOptionsValidator().Validate(
            name: null,
            new SteamGiftsOptions());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("SteamGifts token", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_accepts_complete_options()
    {
        var result = new SteamGiftsOptionsValidator().Validate(
            name: null,
            new SteamGiftsOptions
            {
                Token = "steam-gifts-token",
                Filters = new GiveawayFilterOptions
                {
                    Join = [Range(70, 100, 1_000, int.MaxValue)]
                }
            });

        Assert.True(result.Succeeded);
    }

    private static GiveawayCandidate Game(bool isCollection = false, bool isAlreadyJoined = false) => new()
    {
        Title = "Example game",
        GiveawayUrl = "/giveaway/example",
        ApplicationId = "123",
        IsCollection = isCollection,
        IsAlreadyJoined = isAlreadyJoined,
        RequiredLevel = 1
    };

    private static GiveawayUser User() => new() { Username = "player", Level = 1, Points = 100 };

    private static AppReview Review(double rating, int reviews) => new()
    {
        Rating = rating,
        TotalReviews = reviews
    };

    private static GiveawayFilterOptions Filters(
        bool enterCollections = false,
        List<RatingReviewRange>? join = null,
        List<RatingReviewRange>? hide = null) => new()
        {
            EnterCollections = enterCollections,
            Join = join ?? [],
            Hide = hide ?? []
        };

    private static RatingReviewRange Range(int ratingFrom, int ratingTo, int reviewsFrom, int reviewsTo) => new()
    {
        RatingFrom = ratingFrom,
        RatingTo = ratingTo,
        ReviewsFrom = reviewsFrom,
        ReviewsTo = reviewsTo
    };
}
