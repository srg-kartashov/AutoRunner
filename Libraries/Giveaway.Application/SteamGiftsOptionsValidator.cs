using Giveaway.Contracts.Options;

using Microsoft.Extensions.Options;

namespace Giveaway.Application;

public sealed class SteamGiftsOptionsValidator : IValidateOptions<SteamGiftsOptions>
{
    public ValidateOptionsResult Validate(string? name, SteamGiftsOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Token))
            failures.Add("SteamGifts token is not configured.");

        if (options.ActionDelaySeconds < 0)
            failures.Add("SteamGifts action delay cannot be negative.");

        if (options.Browser is null)
        {
            failures.Add("SteamGifts browser configuration is missing.");
        }
        else if (string.IsNullOrWhiteSpace(options.Browser.UserDataDirectory))
        {
            failures.Add("SteamGifts browser user-data directory is not configured.");
        }

        if (options.Filters is null)
        {
            failures.Add("SteamGifts filter configuration is missing.");
        }
        else
        {
            ValidateRanges(options.Filters.Join, "join", failures);
            ValidateRanges(options.Filters.Hide, "hide", failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRanges(
        IEnumerable<RatingReviewRange>? ranges,
        string rangeType,
        List<string> failures)
    {
        if (ranges is null)
        {
            failures.Add($"SteamGifts {rangeType} filter list is missing.");
            return;
        }

        foreach (var range in ranges)
        {
            if (range is null)
            {
                failures.Add($"SteamGifts {rangeType} filter contains an empty range.");
                continue;
            }

            if (range.RatingFrom < 0 || range.RatingTo > 100 || range.RatingFrom > range.RatingTo)
                failures.Add($"SteamGifts {rangeType} rating filter range must be between 0 and 100.");

            if (range.ReviewsFrom < 0 || range.ReviewsFrom > range.ReviewsTo)
                failures.Add($"SteamGifts {rangeType} review filter range is invalid.");
        }
    }
}
