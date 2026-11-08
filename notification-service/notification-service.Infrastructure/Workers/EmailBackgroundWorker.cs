using common_library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using notification_service.Application.Services;
using notification_service.Domain.Enums;
using notification_service.Infrastructure.Persistance;

namespace notification_service.Infrastructure.Workers;

public class EmailBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

    public EmailBackgroundWorker(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    protected override async Task<Result> ExecuteAsync(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested && await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception)
            {
                return Result.Failure(-1);
            }
        }
        return Result.Success();
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var pendingLogs = await dbContext.NotificationLogs
                                         .Where(x => x.Status != NotificationStatus.Sent)
                                         .Take(20)
                                         .ToListAsync(cancellationToken);

        if (!pendingLogs.Any())
        {
            return;
        }

        foreach (var log in pendingLogs)
        {
            try
            {
                string body = $"Hello! You have been invited to a meeting by <b>{log.AuthorEmail}</b>. For {log.Subject}.";

                await emailSender.SendEmailAsync(
                    new List<string> { log.RecipientEmail },
                    log.Subject,
                    body,
                    cancellationToken);

                log.Status = NotificationStatus.Sent;
                log.SentAt = DateTime.UtcNow;
                log.ErrorMessage = null;

            }
            catch (Exception ex)
            {
                log.Status = NotificationStatus.Failed;
                log.ErrorMessage = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
