using Microsoft.Extensions.Caching.Memory;

using SteamPowered.Client;
using SteamPowered.Client.Models;

namespace AutoRunner.Services
{
    public class SteamPoweredCachedService : ISteamPoweredClient
    {
        private readonly ISteamPoweredClient _client;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SteamPoweredCachedService>? _logger;

        public SteamPoweredCachedService(HttpClient httpClient, IMemoryCache cache, ILogger<SteamPoweredCachedService>? logger = null)
        {
            _client = new SteamPoweredClient(httpClient, logger);
            _cache = cache;
            _logger = logger;
        }

        public async Task<AppReviews?> GetAppReviewsAsync(string applicationId)
        {
            var cacheKey = $"AppReviews:{applicationId}";

            if (_cache.TryGetValue(cacheKey, out AppReviews? cached))
            {
                _logger?.LogTrace("Cache hit for app {AppId}", applicationId);
                return cached;
            }
            _logger?.LogTrace("Cache miss for app {AppId}, fetching from Steam", applicationId);
            var result = await _client.GetAppReviewsAsync(applicationId);

            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromDays(7));
                _logger?.LogTrace("App reviews for {AppId} cached for 7 days", applicationId);
            }
            else
            {
                _logger?.LogWarning("Failed to fetch app reviews for {AppId}", applicationId);
            }

            return result;
        }
    }
}
