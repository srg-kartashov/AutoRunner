using System.Net;
using System.Net.Http;
using System.Text.Json;

using SteamPowered.Client;

using Xunit;

namespace Giveaway.Application.Tests;

public sealed class SteamPoweredClientTests
{
    [Theory]
    [InlineData(26, 68, 38.23529411764706)]
    [InlineData(0, 0, 0)]
    public async Task GetAppReviewsAsync_uses_web_api_and_preserves_rating(int positive, int total, double expectedRating)
    {
        var json = JsonSerializer.Serialize(new
        {
            response = new
            {
                query_summary = new { total_positive = positive, total_negative = total - positive, total_reviews = total }
            }
        });
        using var handler = new ReviewsHandler(json);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://store.steampowered.com")
        };
        var client = new SteamPoweredClient(httpClient);

        var review = await client.GetAppReviewsAsync("315320");

        Assert.NotNull(review);
        Assert.Equal(total, review.TotalReviews);
        Assert.Equal(expectedRating, review.Rating, 10);
        Assert.Equal(HttpMethod.Get, handler.Method);
        var requestUri = Assert.IsType<Uri>(handler.RequestUri);
        Assert.Equal("https://api.steampowered.com/IUserReviewsService/GetAppReviews/v1/", requestUri.GetLeftPart(UriPartial.Path));
        Assert.StartsWith("?input_json=%7B", requestUri.Query);
        using var input = JsonDocument.Parse(Uri.UnescapeDataString(requestUri.Query["?input_json=".Length..]));
        Assert.Equal(315320u, input.RootElement.GetProperty("appid").GetUInt32());
        Assert.Equal("all", Assert.Single(input.RootElement.GetProperty("languages").EnumerateArray()).GetString());
        Assert.Equal(2, input.RootElement.EnumerateObject().Count());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"response\":{}}")]
    [InlineData("{\"query_summary\":{\"total_positive\":26,\"total_reviews\":68}}")]
    public async Task GetAppReviewsAsync_returns_null_without_nested_summary(string json)
    {
        using var handler = new ReviewsHandler(json);
        using var httpClient = new HttpClient(handler);
        var client = new SteamPoweredClient(httpClient);

        Assert.Null(await client.GetAppReviewsAsync("315320"));
    }

    private sealed class ReviewsHandler(string json) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }
}
