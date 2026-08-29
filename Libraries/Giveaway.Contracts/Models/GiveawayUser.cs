namespace Giveaway.Contracts.Models;

public sealed class GiveawayUser
{
    public string Username { get; init; } = string.Empty;
    public int Points { get; init; }
    public int Level { get; init; }
}
