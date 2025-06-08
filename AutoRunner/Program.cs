using Hangfire;
using Hangfire.Dashboard;
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

            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // Hangfire Dashboard (optional - remove or secure in prod)
            app.UseHangfireDashboard(options: new DashboardOptions
            {
                Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
            });

            app.Run();
        }
    }
    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true; 
        }
    }
}
