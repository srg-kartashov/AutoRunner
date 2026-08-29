using Giveaway.Contracts.Models;

namespace Giveaway.Contracts.Ports;

public interface IAppReviewsCache
{
    Task<AppReview?> GetAsync(string applicationId, CancellationToken cancellationToken = default);
    Task SetAsync(string applicationId, AppReview review, CancellationToken cancellationToken = default);
}
