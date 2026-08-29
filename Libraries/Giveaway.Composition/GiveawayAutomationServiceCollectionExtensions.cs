using Giveaway.Application;
using Giveaway.Contracts.Options;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SteamGifts.Client;
using SteamPowered.Client;

namespace Giveaway.Composition;

public static class GiveawayAutomationServiceCollectionExtensions
{
    public static IServiceCollection AddGiveawayAutomation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<SteamGiftsOptions>()
            .Bind(configuration.GetSection(SteamGiftsOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SteamGiftsOptions>, SteamGiftsOptionsValidator>();
        services.AddMemoryCache();
        services.AddHttpClient<IAppReviewsProvider, SteamPoweredCachedReviewsProvider>();
        services.AddSingleton<ISteamGiftsSessionFactory, PlaywrightSteamGiftsSessionFactory>();
        services.AddSingleton<GiveawayDecisionService>();
        services.AddScoped<SteamGiftsAutomationService>();

        return services;
    }
}
