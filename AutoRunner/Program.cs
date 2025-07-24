using AutoRunner.Factories;
using AutoRunner.Jobs;
using AutoRunner.Services;

using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.MissionControl;
using Hangfire.PostgreSql;
using Hangfire.RecurringJobExtensions;

using Microsoft.Playwright;

using SteamPowered.Client;

using System.Threading.Tasks;

using TelegramNotifier.Client.Extensions;

namespace AutoRunner
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine($"Conn string: {connStr}");

            builder.Services.AddHangfire(config =>
                    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UsePostgreSqlStorage(
                        options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")),
                        new PostgreSqlStorageOptions
                        {
                            SchemaName = "hangfire"
                        })
                      .UseRecurringJob(typeof(SteamGiftsJoinJob))
                      .UseConsole()
            .UseMissionControl(new MissionControlOptions()
            {
                RequireConfirmation = false,    // Отключение подтверждения для запуска задач
                HideCodeSnippet = false         // Отображение кода задачи
            },
            typeof(SteamGiftsJoinJob).Assembly)
            );

            builder.Services.AddHangfireServer(options =>
            {
                options.ServerName = builder.Configuration["Hangfire:ServerName"] ?? "default-server";
                options.WorkerCount = 1; // Количество воркеров, обрабатывающих задачи
            });

            builder.Services.AddTelegramNotifier(builder.Configuration);

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient<ISteamPoweredClient, SteamPoweredCachedService>();

            builder.Services.AddSingleton<IPlaywrightDriverFactory, PlaywrightDriverFactory>();

#if RELEASE
            builder.WebHost.UseUrls("http://0.0.0.0:5000");
#endif

            builder.Services.AddHangfireConsoleExtensions();

            var app = builder.Build();

            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

#if DEBUG
            app.UseHangfireDashboard();
#else
            app.UseHangfireDashboard(options: new DashboardOptions()
            {
                DashboardTitle = "AutoRunner",
                FaviconPath = "/favicon.ico",
                DarkModeEnabled = true,
                Authorization =
                [
                    new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions()
                    {
                        RequireSsl = false,
                        SslRedirect = false,
                        LoginCaseSensitive = true,
                        Users = new []
                        {
                            new BasicAuthAuthorizationUser()
                            {
                                Login = builder.Configuration["Hangfire:Login"],
                                PasswordClear = builder.Configuration["Hangfire:Password"]
                            }
                        }
                    })
                ]
            });
#endif

            app.Run();
        }
    }
}
