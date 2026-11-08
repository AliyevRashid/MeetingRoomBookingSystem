using common_library.Contracts.Events;
using MassTransit;
using notification_service.Domain.Entities;
using notification_service.Domain.Enums;
using notification_service.Infrastructure.Persistance;


namespace notification_service.Infrastructure.Consumers;

public class BookingNotificationConsumer : IConsumer<BookingNotificationEvent>
{
    private readonly NotificationDbContext _dbContext;
    public BookingNotificationConsumer(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task Consume(ConsumeContext<BookingNotificationEvent> context)
    {
        var message = context.Message;

        var recipientsEmails = message.RecipientsEmails.Distinct().ToList();

        var logs = recipientsEmails.Select(email => new NotificationLog
        {
            Id = Guid.NewGuid(),
            BookingId = message.BookingId,
            RecipientEmail = email,
            AuthorEmail = message.AuthorEmail,
            Subject = message.Subject,
            Status = NotificationStatus.Pending
        }).ToList();

        await _dbContext.NotificationLogs.AddRangeAsync(logs);
        await _dbContext.SaveChangesAsync();
    }
}
