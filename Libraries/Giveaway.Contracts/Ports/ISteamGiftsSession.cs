using Giveaway.Contracts.Models;

namespace Giveaway.Contracts.Ports;

public interface ISteamGiftsSession : IAsyncDisposable
{
    Task AuthenticateAsync(string token, CancellationToken cancellationToken = default);
    Task<GiveawayUser> GetUserAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GiveawayCandidate>> GetGiveawaysAsync(CancellationToken cancellationToken = default);
    Task<bool> JoinGiveawayAsync(string giveawayUrl, CancellationToken cancellationToken = default);
    Task<bool> HideGiveawayAsync(string giveawayUrl, CancellationToken cancellationToken = default);
}
