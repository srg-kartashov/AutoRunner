using Giveaway.Application;
using Giveaway.Contracts.Models;
using Giveaway.Contracts.Options;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Giveaway.Application.Tests;

public sealed class SteamGiftsAutomationServiceTests
{
    [Fact]
    public async Task RunAsync_continues_after_an_unaffordable_giveaway_when_points_remain()
    {
        var expensiveGiveaway = Giveaway("expensive", points: 20);
        var affordableGiveaway = Giveaway("affordable", points: 10);
        var session = new FakeSession(
            user: new GiveawayUser { Username = "player", Level = 1, Points = 10 },
            giveaways: [expensiveGiveaway, affordableGiveaway]);
        var service = new SteamGiftsAutomationService(
            new FakeSessionFactory(session),
            new FakeReviewsProvider(),
            new GiveawayDecisionService(),
            NullLogger<SteamGiftsAutomationService>.Instance,
            Options.Create(AutomationOptions()));

        var summary = await service.RunAsync();

        Assert.Equal(1, summary.InsufficientPoints);
        Assert.Equal(1, summary.Joined);
        Assert.Equal(0, summary.RemainingPoints);
        Assert.Equal([affordableGiveaway.GiveawayUrl], session.JoinedGiveawayUrls);
    }

    private static SteamGiftsOptions AutomationOptions() => new()
    {
        Token = "steam-gifts-token",
        ActionDelaySeconds = 0,
        Filters = new GiveawayFilterOptions
        {
            StopWhenOutOfPoints = true,
            Join = [new RatingReviewRange { RatingFrom = 0, RatingTo = 100, ReviewsFrom = 0, ReviewsTo = int.MaxValue }]
        }
    };

    private static GiveawayCandidate Giveaway(string applicationId, int points) => new()
    {
        Title = applicationId,
        GiveawayUrl = $"/giveaway/{applicationId}",
        ApplicationId = applicationId,
        Points = points,
        RequiredLevel = 1
    };

    private sealed class FakeSessionFactory(FakeSession session) : ISteamGiftsSessionFactory
    {
        public Task<ISteamGiftsSession> CreateAsync(
            SteamGiftsBrowserOptions browserOptions,
            CancellationToken cancellationToken = default) => Task.FromResult<ISteamGiftsSession>(session);
    }

    private sealed class FakeSession(GiveawayUser user, IReadOnlyList<GiveawayCandidate> giveaways) : ISteamGiftsSession
    {
        public List<string> JoinedGiveawayUrls { get; } = [];

        public Task AuthenticateAsync(string token, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<GiveawayUser> GetUserAsync(CancellationToken cancellationToken = default) => Task.FromResult(user);

        public Task<IReadOnlyList<GiveawayCandidate>> GetGiveawaysAsync(CancellationToken cancellationToken = default) => Task.FromResult(giveaways);

        public Task<bool> JoinGiveawayAsync(string giveawayUrl, CancellationToken cancellationToken = default)
        {
            JoinedGiveawayUrls.Add(giveawayUrl);
            return Task.FromResult(true);
        }

        public Task<bool> HideGiveawayAsync(string giveawayUrl, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeReviewsProvider : IAppReviewsProvider
    {
        public Task<AppReview?> GetAppReviewsAsync(string applicationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AppReview?>(new AppReview { Rating = 90, TotalReviews = 1_000 });
    }
}
