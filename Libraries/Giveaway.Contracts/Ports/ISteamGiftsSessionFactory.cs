using Giveaway.Contracts.Options;

namespace Giveaway.Contracts.Ports;

public interface ISteamGiftsSessionFactory
{
    Task<ISteamGiftsSession> CreateAsync(
        SteamGiftsBrowserOptions browserOptions,
        CancellationToken cancellationToken = default);
}
