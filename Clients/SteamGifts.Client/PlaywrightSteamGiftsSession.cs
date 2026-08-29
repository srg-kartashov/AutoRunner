using Giveaway.Contracts.Models;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using SteamGifts.Client.Models;

namespace SteamGifts.Client;

public sealed class PlaywrightSteamGiftsSession : ISteamGiftsSession
{
    private readonly IPlaywright _playwright;
    private readonly IBrowserContext _browserContext;
    private readonly SteamGiftsClient _client;

    public PlaywrightSteamGiftsSession(
        IPlaywright playwright,
        IBrowserContext browserContext,
        IPage page,
        ILogger logger)
    {
        _playwright = playwright;
        _browserContext = browserContext;
        _client = new SteamGiftsClient(page, logger);
    }

    public async Task AuthenticateAsync(string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _client.AuthAsync(token);
    }

    public async Task<GiveawayUser> GetUserAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _client.GetUserInfoAsync();
        return MapUser(user);
    }

    public async Task<IReadOnlyList<GiveawayCandidate>> GetGiveawaysAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var giveaways = await _client.GetAllGiveawaysAsync();
        return giveaways.Select(MapGiveaway).ToArray();
    }

    public async Task<bool> JoinGiveawayAsync(string giveawayUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _client.JoinGiveawayAsync(giveawayUrl);
    }

    public async Task<bool> HideGiveawayAsync(string giveawayUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _client.HideGiveawayAsync(giveawayUrl);
    }

    public async ValueTask DisposeAsync()
    {
        await _browserContext.CloseAsync();
        _playwright.Dispose();
    }

    private static GiveawayUser MapUser(SteamGiftsUserInfo user) => new()
    {
        Username = user.Username,
        Points = user.Points,
        Level = user.Level
    };

    private static GiveawayCandidate MapGiveaway(SteamGiftsGiveaway giveaway) => new()
    {
        Title = giveaway.GameName,
        GiveawayUrl = giveaway.GiveawayUrl,
        StoreUrl = giveaway.SteamUrl,
        Points = giveaway.Points,
        RequiredLevel = giveaway.Level,
        ApplicationId = giveaway.ApplicationId,
        IsAlreadyJoined = giveaway.Joined,
        IsCollection = giveaway.IsCollection
    };
}
