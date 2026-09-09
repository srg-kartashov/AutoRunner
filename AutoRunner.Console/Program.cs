using Giveaway.Application;
using Giveaway.Composition;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TelegramNotifier.Client;
using TelegramNotifier.Client.Extensions;

using Velopack;

namespace AutoRunner.ConsoleApp;

public class Program
{
    public static int Main(string[] args)
    {
        VelopackApp.Build().Run();

        return RunAsync(args).GetAwaiter().GetResult();
    }

    private static async Task<int> RunAsync(string[] args)
    {

        Console.Title = "SteamGifts";
        WriteStartupLogo();

        using var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        using var host = BuildHost(args);
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AutoRunner.Console");

        if (await SteamGiftsUpdater.TryUpdateAndRestartAsync(logger))
            return 0;

        var hostStarted = false;
        await using var scope = host.Services.CreateAsyncScope();
        var notifier = scope.ServiceProvider.GetService<ITelegramNotifier<Program>>();

        try
        {
            await host.StartAsync(cancellationSource.Token);
            hostStarted = true;

            var automationService = scope.ServiceProvider.GetRequiredService<SteamGiftsAutomationService>();

            logger.LogInformation("Starting SteamGifts console run");
            await SendNotificationAsync(notifier, "Starting SteamGifts console run.", logger);

            var summary = await automationService.RunAsync(cancellationSource.Token);
            Console.WriteLine(summary.ToDisplayText());
            await SendNotificationAsync(notifier, summary.ToDisplayText(), logger);
            return 0;
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
            logger.LogWarning("SteamGifts console run was cancelled");
            return 2;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "SteamGifts console run failed");
            await SendNotificationAsync(notifier, $"SteamGifts console run failed:\n<pre>{exception.Message}</pre>", logger);
            return 1;
        }
        finally
        {
            if (hostStarted)
                await host.StopAsync(CancellationToken.None);
        }
    }

    private static IHost BuildHost(string[] args)
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var applicationData = SteamGiftsApplicationData.Initialize(executableDirectory);
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = executableDirectory
        });
        builder.Configuration
            .AddJsonFile(applicationData.SettingsFilePath, optional: false, reloadOnChange: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SteamGifts:Browser:UserDataDirectory"] = applicationData.BrowserProfileDirectory,
                ["SteamPowered:ReviewsCache:DatabasePath"] = applicationData.ReviewsCacheFilePath
            })
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddSimpleConsole();

        builder.Services.AddGiveawayAutomation(builder.Configuration, AppReviewsCacheStorage.Sqlite);

        var telegramSection = builder.Configuration.GetSection("TelegramNotifier");
        if (!string.IsNullOrWhiteSpace(telegramSection["BotToken"]) &&
            !string.IsNullOrWhiteSpace(telegramSection["GroupId"]))
        {
            builder.Services.AddTelegramNotifier(builder.Configuration);
        }

        return builder.Build();
    }

    private static async Task SendNotificationAsync(
        ITelegramNotifier<Program>? notifier,
        string message,
        ILogger logger)
    {
        if (notifier is null)
            return;

        try
        {
            await notifier.SendTextAsync(message);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to send console notification to Telegram");
        }
    }

    private static void WriteStartupLogo()
    {
        const string logo = """
             _____ _                       _____ _  __ _       
            / ____| |                     / ____| |/ _| |      
           | (___ | |_ ___  __ _ _ __ ___ | |  __| | |_| |_ ___ 
            \___ \| __/ _ \/ _` | '_ ` _ \| | |_ | |  _| __/ __|
            ____) | ||  __/ (_| | | | | | | |__| | | | | |_\__ \
           |_____/ \__\___|\__,_|_| |_| |_|\_____|_|_|  \__|___/
        """;

        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine(logo);
        Console.ForegroundColor = previousColor;
    }
}
