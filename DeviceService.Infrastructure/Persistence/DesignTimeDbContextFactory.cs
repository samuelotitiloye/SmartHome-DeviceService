using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DeviceService.Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DeviceDbContext>
    {
        public DeviceDbContext CreateDbContext(string[] args)
        {
            // Move to API project folder where appsettings.json exists
            var basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "DeviceService.Api"
            );

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

            var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new Exception("Connection string 'DefaultConnection' not found.");

            var connectionString = rawConnectionString
                .Replace("${DB_USER}", dbUser)
                .Replace("${DB_PASSWORD}", dbPassword);

            var optionsBuilder = new DbContextOptionsBuilder<DeviceDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new DeviceDbContext(optionsBuilder.Options);
        }
    }
}
