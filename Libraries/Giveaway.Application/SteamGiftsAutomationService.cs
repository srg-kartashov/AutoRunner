using Giveaway.Contracts.Models;
using Giveaway.Contracts.Options;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Giveaway.Application;

public sealed class SteamGiftsAutomationService
{
    private readonly ISteamGiftsSessionFactory _sessionFactory;
    private readonly IAppReviewsProvider _reviewsProvider;
    private readonly GiveawayDecisionService _decisionService;
    private readonly ILogger<SteamGiftsAutomationService> _logger;
    private readonly IOptions<SteamGiftsOptions> _options;

    public SteamGiftsAutomationService(
        ISteamGiftsSessionFactory sessionFactory,
        IAppReviewsProvider reviewsProvider,
        GiveawayDecisionService decisionService,
        ILogger<SteamGiftsAutomationService> logger,
        IOptions<SteamGiftsOptions> options)
    {
        _sessionFactory = sessionFactory;
        _reviewsProvider = reviewsProvider;
        _decisionService = decisionService;
        _logger = logger;
        _options = options;
    }

    public async Task<GiveawayRunSummary> RunAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        await using var session = await _sessionFactory.CreateAsync(options.Browser, cancellationToken);
        await session.AuthenticateAsync(options.Token, cancellationToken);

        var user = await session.GetUserAsync(cancellationToken);
        var giveaways = await session.GetGiveawaysAsync(cancellationToken);
        var summary = new GiveawayRunSummary
        {
            User = user,
            TotalGiveaways = giveaways.Count,
            RemainingPoints = user.Points
        };

        var joinCandidates = new List<GiveawayDecision>();
        var hideCandidates = new List<GiveawayDecision>();

        foreach (var giveaway in giveaways)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AppReview? review = null;
            if (!giveaway.IsCollection && !string.IsNullOrWhiteSpace(giveaway.ApplicationId))
                review = await _reviewsProvider.GetAppReviewsAsync(giveaway.ApplicationId, cancellationToken);

            var decision = _decisionService.Decide(giveaway, user, review, options.Filters);
            LogDecision(decision);

            switch (decision.Action)
            {
                case GiveawayDecisionAction.Join:
                    joinCandidates.Add(decision);
                    break;
                case GiveawayDecisionAction.Hide:
                    hideCandidates.Add(decision);
                    break;
                default:
                    AddSkippedDecision(summary, decision.Reason);
                    break;
            }
        }

        foreach (var decision in joinCandidates.OrderByDescending(candidate => candidate.Score))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (summary.RemainingPoints < decision.Giveaway.Points)
            {
                summary.InsufficientPoints++;
                _logger.LogInformation(
                    "Not enough points for {GameName}: required {RequiredPoints}, available {AvailablePoints}",
                    decision.Giveaway.Title,
                    decision.Giveaway.Points,
                    summary.RemainingPoints);

                if (options.Filters.StopWhenOutOfPoints && summary.RemainingPoints == 0)
                    return summary;

                continue;
            }

            var joined = await session.JoinGiveawayAsync(decision.Giveaway.GiveawayUrl, cancellationToken);
            if (joined)
            {
                summary.Joined++;
                summary.RemainingPoints -= decision.Giveaway.Points;
            }
            else
            {
                summary.FailedJoins++;
            }

            await DelayBetweenActionsAsync(options.ActionDelaySeconds, cancellationToken);
        }

        foreach (var decision in hideCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hidden = await session.HideGiveawayAsync(decision.Giveaway.GiveawayUrl, cancellationToken);
            if (hidden)
            {
                summary.Hidden++;
            }
            else
            {
                summary.FailedHides++;
            }

            await DelayBetweenActionsAsync(options.ActionDelaySeconds, cancellationToken);
        }

        return summary;
    }

    private void LogDecision(GiveawayDecision decision)
    {
        _logger.LogInformation(
            "{Action} {GameName}: {Reason}; rating {Rating}; reviews {Reviews}; score {Score:F2}",
            decision.Action,
            decision.Giveaway.Title,
            decision.Reason,
            decision.Review?.Rating,
            decision.Review?.TotalReviews,
            decision.Score);
    }

    private static void AddSkippedDecision(GiveawayRunSummary summary, GiveawayDecisionReason reason)
    {
        switch (reason)
        {
            case GiveawayDecisionReason.AlreadyJoined:
                summary.AlreadyJoined++;
                break;
            case GiveawayDecisionReason.RequiredLevelNotMet:
                summary.SkippedByLevel++;
                break;
            case GiveawayDecisionReason.CollectionDisabled:
                summary.SkippedCollections++;
                break;
            case GiveawayDecisionReason.MissingApplicationId:
            case GiveawayDecisionReason.MissingReviewData:
                summary.MissingReviewData++;
                break;
            default:
                summary.SkippedByFilter++;
                break;
        }
    }

    private static Task DelayBetweenActionsAsync(int delaySeconds, CancellationToken cancellationToken) =>
        delaySeconds > 0
            ? Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken)
            : Task.CompletedTask;
}
