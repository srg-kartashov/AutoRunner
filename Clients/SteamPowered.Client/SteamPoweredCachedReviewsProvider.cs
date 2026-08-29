using Giveaway.Contracts.Models;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SteamPowered.Client;

public sealed class SteamPoweredCachedReviewsProvider : IAppReviewsProvider
{
    private readonly SteamPoweredClient _client;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SteamPoweredCachedReviewsProvider> _logger;

    public SteamPoweredCachedReviewsProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<SteamPoweredCachedReviewsProvider> logger)
    {
        _client = new SteamPoweredClient(httpClient, logger);
        _cache = cache;
        _logger = logger;
    }

    public async Task<AppReview?> GetAppReviewsAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"AppReviews:{applicationId}";
        if (_cache.TryGetValue(cacheKey, out AppReview? cached))
        {
            _logger.LogTrace("Cache hit for app {AppId}", applicationId);
            return cached;
        }

        _logger.LogTrace("Cache miss for app {AppId}, fetching from Steam", applicationId);
        var result = await _client.GetAppReviewsAsync(applicationId, cancellationToken);

        if (result is not null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromDays(7));
            return result;
        }

        _logger.LogWarning("Failed to fetch app reviews for {AppId}", applicationId);
        return null;
    }
}
