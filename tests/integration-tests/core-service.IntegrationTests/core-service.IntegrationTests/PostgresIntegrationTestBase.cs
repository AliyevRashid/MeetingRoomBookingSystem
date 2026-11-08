using core_service.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

public abstract class PostgresIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
                                                                   .WithImage("postgres:15-alpine")
                                                                   .WithDatabase("core_service_integration_db")
                                                                   .WithUsername("postgres")
                                                                   .WithPassword("postgres")
                                                                   .Build();

    protected CoreServiceDbContext DbContext { get; private set; } = null!;
    protected string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        ConnectionString = _postgresContainer.GetConnectionString();

        var options = new DbContextOptionsBuilder<CoreServiceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        DbContext = new CoreServiceDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }
}