using Giveaway.Contracts.Models;
using Giveaway.Contracts.Ports;

using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SteamReviews.DataBase.Models;

namespace SteamReviews.DataBase;

public sealed class EfCoreAppReviewsCache : IAppReviewsCache
{
    private readonly IDbContextFactory<SteamReviewsCacheDbContext> _dbContextFactory;
    private readonly ILogger<EfCoreAppReviewsCache> _logger;
    private readonly TimeSpan _expiration;
    private readonly Lock _initializationLock = new();
    private Task? _initializationTask;

    public EfCoreAppReviewsCache(
        IDbContextFactory<SteamReviewsCacheDbContext> dbContextFactory,
        SteamReviewsCacheOptions options,
        ILogger<EfCoreAppReviewsCache> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _expiration = TimeSpan.FromDays(options.ExpirationDays);
    }

    public async Task<AppReview?> GetAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureDatabaseAsync();

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entry = await dbContext.AppReviews
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    entry => entry.ApplicationId == applicationId && entry.ExpiresAtUnixSeconds > now,
                    cancellationToken);

            return entry is null
                ? null
                : new AppReview
                {
                    TotalReviews = entry.TotalReviews,
                    Rating = entry.Rating
                };
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Unable to read the cached Steam reviews for app {AppId}", applicationId);
            return null;
        }
    }

    public async Task SetAsync(string applicationId, AppReview review, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureDatabaseAsync();

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entry = await dbContext.AppReviews.SingleOrDefaultAsync(entry => entry.ApplicationId == applicationId, cancellationToken);

            if (entry is null)
            {
                dbContext.AppReviews.Add(new SteamReviewsCacheEntry
                {
                    ApplicationId = applicationId,
                    TotalReviews = review.TotalReviews,
                    Rating = review.Rating,
                    ExpiresAtUnixSeconds = now + (long)_expiration.TotalSeconds
                });
            }
            else
            {
                entry.TotalReviews = review.TotalReviews;
                entry.Rating = review.Rating;
                entry.ExpiresAtUnixSeconds = now + (long)_expiration.TotalSeconds;
            }

            await dbContext.AppReviews
                .Where(entry => entry.ApplicationId != applicationId && entry.ExpiresAtUnixSeconds <= now)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Unable to cache Steam reviews for app {AppId}", applicationId);
        }
    }

    private Task EnsureDatabaseAsync()
    {
        lock (_initializationLock)
            return _initializationTask ??= InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
