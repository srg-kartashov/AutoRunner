using System.Net;
using System.Net.Http;
using System.Threading;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using SteamPowered.Client;
using SteamReviews.DataBase;

using Xunit;

namespace Giveaway.Application.Tests;

public sealed class CachedAppReviewsProviderTests
{
    [Fact]
    public async Task GetAppReviewsAsync_reuses_a_review_from_a_previous_provider_instance()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"steam-reviews-{Guid.NewGuid():N}.db");
        var handler = new SteamReviewsHandler();

        try
        {
            using (var httpClient = new HttpClient(handler))
            using (var serviceProvider = new ServiceCollection()
                       .AddDbContextFactory<SteamReviewsCacheDbContext>(options => options.UseSqlite($"Data Source={databasePath};Pooling=False"))
                       .BuildServiceProvider())
            {
                var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<SteamReviewsCacheDbContext>>();
                var cacheOptions = new SteamReviewsCacheOptions
                {
                    DatabasePath = databasePath,
                    ExpirationDays = 7
                };
                var firstProvider = CreateProvider(httpClient, dbContextFactory, cacheOptions);

                var firstReview = await firstProvider.GetAppReviewsAsync("123");

                var secondProvider = CreateProvider(httpClient, dbContextFactory, cacheOptions);
                var secondReview = await secondProvider.GetAppReviewsAsync("123");

                Assert.Equal(90, firstReview!.Rating);
                Assert.Equal(firstReview.Rating, secondReview!.Rating);
                Assert.Equal(1, handler.RequestCount);
            }
        }
        finally
        {
            DeleteIfExists(databasePath);
            DeleteIfExists($"{databasePath}-shm");
            DeleteIfExists($"{databasePath}-wal");
        }
    }

    private static CachedAppReviewsProvider CreateProvider(
        HttpClient httpClient,
        IDbContextFactory<SteamReviewsCacheDbContext> dbContextFactory,
        SteamReviewsCacheOptions cacheOptions)
    {
        return new CachedAppReviewsProvider(
            new SteamPoweredClient(httpClient),
            new EfCoreAppReviewsCache(
                dbContextFactory,
                cacheOptions,
                NullLogger<EfCoreAppReviewsCache>.Instance),
            NullLogger<CachedAppReviewsProvider>.Instance);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private sealed class SteamReviewsHandler : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => _requestCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"query_summary":{"total_positive":90,"total_reviews":100}}""")
            });
        }
    }
}
