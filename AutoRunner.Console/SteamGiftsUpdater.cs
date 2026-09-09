using Microsoft.Extensions.Logging;

using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace AutoRunner.ConsoleApp;

internal static class SteamGiftsUpdater
{
    private const string RepositoryUrl = "https://github.com/srg-kartashov/AutoRunner";

    public static async Task<bool> TryUpdateAndRestartAsync(ILogger logger)
    {
        try
        {
            var updateManager = new UpdateManager(
                new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
            var update = await updateManager.CheckForUpdatesAsync();

            if (update is null)
                return false;

            logger.LogInformation("A newer SteamGifts version is available. Downloading the update.");
            await updateManager.DownloadUpdatesAsync(update);
            updateManager.ApplyUpdatesAndRestart(update);

            return true;
        }
        catch (NotInstalledException)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "SteamGifts update check failed. Continuing with the current version.");
            return false;
        }
    }
}
