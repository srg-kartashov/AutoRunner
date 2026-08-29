using Giveaway.Contracts.Models;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Logging;

namespace SteamPowered.Client;

public sealed class CachedAppReviewsProvider : IAppReviewsProvider
{
    private readonly SteamPoweredClient _source;
    private readonly IAppReviewsCache _cache;
    private readonly ILogger<CachedAppReviewsProvider> _logger;

    public CachedAppReviewsProvider(
        SteamPoweredClient source,
        IAppReviewsCache cache,
        ILogger<CachedAppReviewsProvider> logger)
    {
        _source = source;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AppReview?> GetAppReviewsAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        var cachedReview = await _cache.GetAsync(applicationId, cancellationToken);
        if (cachedReview is not null)
        {
            _logger.LogTrace("Cache hit for app {AppId}", applicationId);
            return cachedReview;
        }

        _logger.LogTrace("Cache miss for app {AppId}, fetching from Steam", applicationId);
        var review = await _source.GetAppReviewsAsync(applicationId, cancellationToken);
        if (review is not null)
            await _cache.SetAsync(applicationId, review, cancellationToken);

        return review;
    }
}
