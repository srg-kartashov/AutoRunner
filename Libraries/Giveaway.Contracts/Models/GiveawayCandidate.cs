namespace Giveaway.Contracts.Models;

public sealed class GiveawayCandidate
{
    public string Title { get; init; } = string.Empty;
    public string GiveawayUrl { get; init; } = string.Empty;
    public string StoreUrl { get; init; } = string.Empty;
    public int Points { get; init; }
    public int RequiredLevel { get; init; }
    public string ApplicationId { get; init; } = string.Empty;
    public bool IsAlreadyJoined { get; init; }
    public bool IsCollection { get; init; }
}
