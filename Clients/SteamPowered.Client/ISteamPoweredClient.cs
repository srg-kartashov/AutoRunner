using SteamPowered.Client.Models;

namespace SteamPowered.Client
{
    public interface ISteamPoweredClient
    {
        Task<AppReviews?> GetAppReviewsAsync(string applicationId);
    }
}