using common_library;
using core_service.Application.Bookings.Commands.RescheduleBooking;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.Domain.Enums;
using core_service.UnitTests.TestBuilders;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Moq;

public class RescheduleBookingCommandHandlerIntegrationTests : PostgresIntegrationTestBase
{
    [Fact]
    public async Task Handle_Should_UpdateBookingTimeAndPublishNotification_When_RequestIsValid()
    {
        // Arrange
        var organization = new OrganizationBuilder()
                                .WithName("Reschedule Org")
                                .WithTaxId("55443322")
                                .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Engineering", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder()
                            .WithName("Boardroom")
                            .WithOrganizationId(organization.Id)
                            .Build();
        await DbContext.MeetingRooms.AddAsync(room);

        var author = new UserBuilder()
                            .WithEmail("reschedule@company.com")
                            .WithOrganizationId(organization.Id)
                            .WithDepartmentId(department.Id)
                            .Build();
        await DbContext.Users.AddAsync(author);
        await DbContext.SaveChangesAsync();

        var initialStart = DateTime.UtcNow.AddDays(1);
        var initialEnd = initialStart.AddHours(1);

        var booking = new BookingBuilder()
                            .WithRoom(room)
                            .WithAuthor(author)
                            .WithSubject("Planning")
                            .WithTime(initialStart, initialEnd)
                            .Build();
        await DbContext.Bookings.AddAsync(booking);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(author.Id);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);
        currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Default);

        var availabilityCheckerMock = new Mock<IBookingAvailabilityChecker>();
        availabilityCheckerMock
            .Setup(a => a.HasOverlapAsync(room.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var notificationPublisherMock = new Mock<IBookingNotificationPublisher>();

        var handler = new RescheduleBookingCommandHandler(
            availabilityCheckerMock.Object,
            DbContext,
            currentUserServiceMock.Object,
            notificationPublisherMock.Object
        );

        var newStart = initialStart.AddHours(2);
        var newEnd = newStart.AddHours(1);
        var command = new RescheduleBookingCommand(booking.Id, newStart, newEnd);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBooking = await DbContext.Bookings.FindAsync(booking.Id);
        updatedBooking.Should().NotBeNull();
        updatedBooking!.StartTime.Should().Be(newStart);
        updatedBooking.EndTime.Should().Be(newEnd);

        notificationPublisherMock.Verify(
            n => n.PublishNotificationAsync(
                It.IsAny<Booking>(),
                "Boardroom",
                "reschedule@company.com",
                organization.Id,
                It.IsAny<IEnumerable<Guid>>(),
                "Planning rescheduled",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_When_TimeSlotHasOverlap()
    {
        // Arrange
        var organization = new OrganizationBuilder().WithName("Org").WithTaxId("99881122").Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Dept", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder().WithOrganizationId(organization.Id).Build();
        await DbContext.MeetingRooms.AddAsync(room);

        var author = new UserBuilder().WithOrganizationId(organization.Id).WithDepartmentId(department.Id).Build();
        await DbContext.Users.AddAsync(author);
        await DbContext.SaveChangesAsync();

        var booking = new BookingBuilder().WithRoom(room).WithAuthor(author).Build();
        await DbContext.Bookings.AddAsync(booking);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(author.Id);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

        var availabilityCheckerMock = new Mock<IBookingAvailabilityChecker>();
        availabilityCheckerMock
            .Setup(a => a.HasOverlapAsync(room.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RescheduleBookingCommandHandler(
            availabilityCheckerMock.Object,
            DbContext,
            currentUserServiceMock.Object,
            Mock.Of<IBookingNotificationPublisher>()
        );

        var command = new RescheduleBookingCommand(booking.Id, DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(1));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Conflict);
    }
}