using Giveaway.Contracts.Ports;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SteamReviews.DataBase;

public static class SteamReviewsDataBaseServiceCollectionExtensions
{
    public static IServiceCollection AddSteamReviewsSqliteCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SteamReviewsCacheOptions>()
            .Bind(configuration.GetSection(SteamReviewsCacheOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DatabasePath),
                "SteamPowered:ReviewsCache:DatabasePath must be configured.")
            .Validate(
                options => options.ExpirationDays > 0,
                "SteamPowered:ReviewsCache:ExpirationDays must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<SteamReviewsCacheOptions>>().Value);

        services.AddDbContextFactory<SteamReviewsCacheDbContext>((serviceProvider, options) =>
        {
            var cacheOptions = serviceProvider.GetRequiredService<SteamReviewsCacheOptions>();
            var databasePath = Path.GetFullPath(cacheOptions.DatabasePath, AppContext.BaseDirectory);
            var databaseDirectory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);

            options.UseSqlite($"Data Source={databasePath};Pooling=False");
        });
        services.AddSingleton<IAppReviewsCache, EfCoreAppReviewsCache>();

        return services;
    }
}
