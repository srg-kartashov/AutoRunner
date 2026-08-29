using Giveaway.Contracts.Models;
using Giveaway.Contracts.Ports;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using SteamPowered.Client.DTOs;

namespace SteamPowered.Client
{
    public class SteamPoweredClient : IAppReviewsProvider
    {
        private const string BaseUrl = "https://store.steampowered.com";
        private readonly HttpClient _httpClient;
        private readonly ILogger<SteamPoweredClient>? _logger;

        public SteamPoweredClient(HttpClient httpClient, ILogger<SteamPoweredClient>? logger = null)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri(BaseUrl);
            _logger = logger;
        }

        public async Task<AppReview?> GetAppReviewsAsync(string applicationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/appreviews/{applicationId}?json=1&language=all", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonConvert.DeserializeObject<AppReviewsDto>(json);

                if (dto?.QuerySummary == null)
                    return null;

                var summary = dto.QuerySummary;
                var rating = summary.TotalReviews > 0
                    ? summary.TotalPositive / (double)summary.TotalReviews * 100.0
                    : 0;

                return new AppReview
                {
                    TotalReviews = summary.TotalReviews,
                    Rating = rating
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "Failed to fetch app reviews for AppId: {AppId}", applicationId);
                return null;
            }
        }
    }
}
