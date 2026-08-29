using Giveaway.Contracts.Models;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Caching.Memory;

namespace Giveaway.Composition;

internal sealed class MemoryAppReviewsCache(IMemoryCache cache) : IAppReviewsCache
{
    private static readonly TimeSpan Expiration = TimeSpan.FromDays(7);

    public Task<AppReview?> GetAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        cache.TryGetValue(applicationId, out AppReview? review);
        return Task.FromResult(review);
    }

    public Task SetAsync(string applicationId, AppReview review, CancellationToken cancellationToken = default)
    {
        cache.Set(applicationId, review, Expiration);
        return Task.CompletedTask;
    }
}