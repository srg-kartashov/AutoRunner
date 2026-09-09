namespace AutoRunner.ConsoleApp;

internal sealed class SteamGiftsApplicationData
{
    private const string ApplicationName = "SteamGifts";
    private const string SettingsFileName = "appsettings.json";
    private const string BrowserProfileDirectoryName = "playwright-user-data";

    private SteamGiftsApplicationData(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        SettingsFilePath = Path.Combine(rootDirectory, SettingsFileName);
        BrowserProfileDirectory = Path.Combine(rootDirectory, BrowserProfileDirectoryName);
        ReviewsCacheFilePath = Path.Combine(rootDirectory, "data", "steam-reviews-cache.db");
    }

    public string RootDirectory { get; }

    public string SettingsFilePath { get; }

    public string BrowserProfileDirectory { get; }

    public string ReviewsCacheFilePath { get; }

    public static SteamGiftsApplicationData Initialize(string executableDirectory)
    {
        var rootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationName);

        Directory.CreateDirectory(rootDirectory);

        var applicationData = new SteamGiftsApplicationData(rootDirectory);
        applicationData.EnsureSettingsFile(executableDirectory);
        applicationData.TryMigrateBrowserProfile(executableDirectory);

        return applicationData;
    }

    private void EnsureSettingsFile(string executableDirectory)
    {
        if (File.Exists(SettingsFilePath))
            return;

        var templateSettingsFilePath = Path.Combine(executableDirectory, SettingsFileName);
        File.Copy(templateSettingsFilePath, SettingsFilePath);
    }

    private void TryMigrateBrowserProfile(string executableDirectory)
    {
        if (Directory.Exists(BrowserProfileDirectory))
            return;

        var legacyProfileDirectories = new[]
        {
            Path.Combine(Environment.CurrentDirectory, BrowserProfileDirectoryName),
            Path.Combine(executableDirectory, BrowserProfileDirectoryName)
        };

        foreach (var legacyProfileDirectory in legacyProfileDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(legacyProfileDirectory) ||
                PathsEqual(legacyProfileDirectory, BrowserProfileDirectory))
            {
                continue;
            }

            try
            {
                CopyDirectory(legacyProfileDirectory, BrowserProfileDirectory);
                return;
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var sourceFilePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFilePath);
            var destinationFilePath = Path.Combine(destinationDirectory, relativePath);
            var destinationFileDirectory = Path.GetDirectoryName(destinationFilePath)!;

            Directory.CreateDirectory(destinationFileDirectory);
            File.Copy(sourceFilePath, destinationFilePath, overwrite: false);
        }
    }

    private static bool PathsEqual(string firstPath, string secondPath) =>
        string.Equals(
            Path.GetFullPath(firstPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(secondPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
