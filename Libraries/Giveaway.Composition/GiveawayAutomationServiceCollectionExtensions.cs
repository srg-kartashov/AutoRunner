using Giveaway.Application;
using Giveaway.Contracts.Options;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SteamGifts.Client;
using SteamPowered.Client;
using SteamReviews.DataBase;

namespace Giveaway.Composition;

public enum AppReviewsCacheStorage
{
    Memory,
    Sqlite
}

public static class GiveawayAutomationServiceCollectionExtensions
{
    public static IServiceCollection AddGiveawayAutomation(
        this IServiceCollection services,
        IConfiguration configuration,
        AppReviewsCacheStorage appReviewsCacheStorage = AppReviewsCacheStorage.Memory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<SteamGiftsOptions>()
            .Bind(configuration.GetSection(SteamGiftsOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SteamGiftsOptions>, SteamGiftsOptionsValidator>();
        AddAppReviewsProvider(services, configuration, appReviewsCacheStorage);
        services.AddSingleton<ISteamGiftsSessionFactory, PlaywrightSteamGiftsSessionFactory>();
        services.AddSingleton<GiveawayDecisionService>();
        services.AddScoped<SteamGiftsAutomationService>();

        return services;
    }

    private static void AddAppReviewsProvider(
        IServiceCollection services,
        IConfiguration configuration,
        AppReviewsCacheStorage appReviewsCacheStorage)
    {
        switch (appReviewsCacheStorage)
        {
            case AppReviewsCacheStorage.Memory:
                services.AddMemoryCache();
                services.AddSingleton<IAppReviewsCache, MemoryAppReviewsCache>();
                break;
            case AppReviewsCacheStorage.Sqlite:
                services.AddSteamReviewsSqliteCache(configuration);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(appReviewsCacheStorage), appReviewsCacheStorage, null);
        }

        services.AddHttpClient<SteamPoweredClient>();
        services.AddTransient<IAppReviewsProvider, CachedAppReviewsProvider>();
    }
}
