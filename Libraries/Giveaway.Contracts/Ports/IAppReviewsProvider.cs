using Giveaway.Contracts.Models;

namespace Giveaway.Contracts.Ports;

public interface IAppReviewsProvider
{
    Task<AppReview?> GetAppReviewsAsync(string applicationId, CancellationToken cancellationToken = default);
}
