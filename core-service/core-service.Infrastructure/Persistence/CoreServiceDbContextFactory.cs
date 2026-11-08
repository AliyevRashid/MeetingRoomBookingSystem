using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace core_service.Infrastructure.Persistence;

public class CoreServiceDbContextFactory : IDesignTimeDbContextFactory<CoreServiceDbContext>
{
    public CoreServiceDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../core-service.Presentation");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(basePath) ? basePath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<CoreServiceDbContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=core_service_db;Username=postgres;Password=postgres";

        builder.UseNpgsql(connectionString);

        return new CoreServiceDbContext(builder.Options);
    }
}
