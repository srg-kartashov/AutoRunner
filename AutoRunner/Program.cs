using Hangfire;
using Hangfire.PostgreSql;

namespace AutoRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();


            builder.Services.AddHangfire(config =>
                    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UsePostgreSqlStorage(
                        options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")),
                        new PostgreSqlStorageOptions
                        {
                            SchemaName = "hangfire"
                        }
                ));

            builder.Services.AddHangfireServer(options =>
            {
                options.ServerName = builder.Configuration["Hangfire:ServerName"] ?? "default-server";
            });

#if RELEASE
            builder.WebHost.UseUrls("http://0.0.0.0:5000");
#endif

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
