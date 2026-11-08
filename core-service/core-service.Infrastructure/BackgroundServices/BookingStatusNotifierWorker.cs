using core_service.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dapper;

namespace core_service.Infrastructure.BackgroundServices;

public class BookingStatusNotifierWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingStatusNotifierWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public BookingStatusNotifierWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingStatusNotifierWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingStatusNotifierWorker started.");

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessRoomStatusChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking booking statuses.");
            }
        }
    }

    private async Task ProcessRoomStatusChangesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IBookingNotificationService>();

        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT DISTINCT 
                b."RoomId", 
                r."OrganizationId", 
                'Busy' AS Status,
                b."Id" AS ActiveBookingId
            FROM "Bookings" b
            INNER JOIN "MeetingRooms" r ON b."RoomId" = r."Id"
            WHERE b."Status" = 1 
              AND b."StartTime" <= NOW() 
              AND b."EndTime" > NOW()
            UNION ALL
            SELECT DISTINCT 
                b."RoomId", 
                r."OrganizationId", 
                'Free' AS Status,
                NULL::uuid AS ActiveBookingId
            FROM "Bookings" b
            INNER JOIN "MeetingRooms" r ON b."RoomId" = r."Id"
            WHERE b."Status" = 1 
              AND b."EndTime" <= NOW() 
              AND b."EndTime" > NOW() - INTERVAL '1 minute';
            """;

        var statusUpdates = await connection.QueryAsync<RoomStatusUpdateDto>(sql);

        foreach (var update in statusUpdates)
        {
            await notificationService.NotifyRoomStatusChangedAsync(
                update.OrganizationId,
                update.RoomId,
                update.Status,
                update.ActiveBookingId,
                cancellationToken);

            _logger.LogInformation(
                "Sent RoomStatusChanged event for RoomId: {RoomId}, Status: {Status}",
                update.RoomId, update.Status);
        }
    }

    private record RoomStatusUpdateDto(
        Guid RoomId,
        Guid OrganizationId,
        string Status,
        Guid? ActiveBookingId);
}