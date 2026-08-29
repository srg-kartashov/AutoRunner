using AutoRunner.Jobs;

using Giveaway.Composition;

using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.MemoryStorage;
using Hangfire.MissionControl;
using Hangfire.PostgreSql;
using Hangfire.RecurringJobExtensions;

using TelegramNotifier.Client.Extensions;

namespace AutoRunner;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddHangfire(config =>
              config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
#if DEBUG
                    .UseMemoryStorage()
#else
                    .UsePostgreSqlStorage(
                        options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")),
                        new PostgreSqlStorageOptions { SchemaName = "hangfire" })
#endif
                  .UseRecurringJob(typeof(SteamGiftsJoinJob))
                  .UseConsole()
        .UseMissionControl(
            new MissionControlOptions
            {
                RequireConfirmation = false,
                HideCodeSnippet = false
            },
            typeof(SteamGiftsJoinJob).Assembly));

        builder.Services.AddHangfireServer(options =>
        {
            options.ServerName = builder.Configuration["Hangfire:ServerName"] ?? "default-server";
            options.WorkerCount = 1;
        });

        builder.Services.AddTelegramNotifier(builder.Configuration);
        builder.Services.AddGiveawayAutomation(builder.Configuration);
        builder.Services.AddHangfireConsoleExtensions();

#if !DEBUG
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
#endif

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.MapControllers();

#if DEBUG
        app.UseHangfireDashboard();
#else
        app.UseHangfireDashboard(options: new DashboardOptions
        {
            DashboardTitle = "AutoRunner",
            FaviconPath = "/favicon.ico",
            DarkModeEnabled = true,
            Authorization =
            [
                new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = false,
                    SslRedirect = false,
                    LoginCaseSensitive = true,
                    Users =
                    [
                        new BasicAuthAuthorizationUser
                        {
                            Login = builder.Configuration["Hangfire:Login"],
                            PasswordClear = builder.Configuration["Hangfire:Password"]
                        }
                    ]
                })
            ]
        });
#endif

        app.Run();
    }
}
