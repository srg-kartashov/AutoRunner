using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using SteamPowered.Client.DTOs;
using SteamPowered.Client.Models;

namespace SteamPowered.Client
{
    public class SteamPoweredClient : ISteamPoweredClient
    {
        private const string BaseUrl = "https://store.steampowered.com";
        private readonly HttpClient _httpClient;
        private readonly ILogger? _logger;

        public SteamPoweredClient(HttpClient httpClient, ILogger? logger = null)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri(BaseUrl);
            _logger = logger;
        }

        public async Task<AppReviews?> GetAppReviewsAsync(string applicationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/appreviews/{applicationId}?json=1&language=all");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var dto = JsonConvert.DeserializeObject<AppReviewsDto>(json);

                if (dto?.QuerySummary == null)
                    return null;

                var summary = dto.QuerySummary;
                var rating = summary.TotalReviews > 0
                    ? summary.TotalPositive / (double)summary.TotalReviews * 100.0
                    : 0;

                return new AppReviews
                {
                    TotalReviews = summary.TotalReviews,
                    Rating = rating
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to fetch app reviews for AppId: {AppId}", applicationId);
                return null;
            }
        }
    }
}
