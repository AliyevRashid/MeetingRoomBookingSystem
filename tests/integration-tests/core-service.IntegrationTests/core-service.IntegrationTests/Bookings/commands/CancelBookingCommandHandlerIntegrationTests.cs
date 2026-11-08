using common_library;
using core_service.Application.Bookings.Commands.CancelBooking;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.Domain.Enums;
using core_service.UnitTests.TestBuilders;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Moq;

public class CancelBookingCommandHandlerIntegrationTests : PostgresIntegrationTestBase
{
    [Fact]
    public async Task Handle_Should_RemoveBookingAndPublishNotification_When_UserIsAuthor()
    {
        // Arrange
        var organization = new OrganizationBuilder()
            .WithName("Cancel Org")
            .WithTaxId("12345678")
            .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Engineering", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder()
            .WithName("Conference Room A")
            .WithOrganizationId(organization.Id)
            .Build();
        await DbContext.MeetingRooms.AddAsync(room);

        var author = new UserBuilder()
                            .WithEmail("author@company.com")
                            .WithOrganizationId(organization.Id)
                            .WithDepartmentId(department.Id)
                            .Build();
        await DbContext.Users.AddAsync(author);
        await DbContext.SaveChangesAsync();

        var booking = new BookingBuilder()
                            .WithRoom(room)
                            .WithAuthor(author)
                            .WithSubject("Team Sync")
                            .Build();
        await DbContext.Bookings.AddAsync(booking);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(author.Id);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);
        currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Default);

        var notificationPublisherMock = new Mock<IBookingNotificationPublisher>();

        var handler = new CancelBookingCommandHandler(
            DbContext,
            currentUserServiceMock.Object,
            notificationPublisherMock.Object
        );

        var command = new CancelBookingCommand(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var bookingInDb = await DbContext.Bookings.FindAsync(booking.Id);
        bookingInDb.Should().BeNull();

        notificationPublisherMock.Verify(
            n => n.PublishNotificationAsync(
                It.IsAny<Booking>(),
                "Conference Room A",
                "author@company.com",
                organization.Id,
                It.IsAny<IEnumerable<Guid>>(),
                "Team Sync cancelled",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_BookingDoesNotExist()
    {
        // Arrange
        var organization = new OrganizationBuilder().WithName("Org").WithTaxId("87654321").Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

        var handler = new CancelBookingCommandHandler(
            DbContext,
            currentUserServiceMock.Object,
            Mock.Of<IBookingNotificationPublisher>()
        );

        var command = new CancelBookingCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthorAndNotAdmin()
    {
        // Arrange
        var organization = new OrganizationBuilder().WithName("Org").WithTaxId("11223344").Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Dept", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder().WithOrganizationId(organization.Id).Build();
        await DbContext.MeetingRooms.AddAsync(room);

        var author = new UserBuilder().WithOrganizationId(organization.Id).WithDepartmentId(department.Id).Build();
        var anotherUser = new UserBuilder().WithOrganizationId(organization.Id).WithDepartmentId(department.Id).Build();
        await DbContext.Users.AddRangeAsync(author, anotherUser);
        await DbContext.SaveChangesAsync();

        var booking = new BookingBuilder().WithRoom(room).WithAuthor(author).Build();
        await DbContext.Bookings.AddAsync(booking);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(anotherUser.Id); // Чужой юзер
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);
        currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Default);

        var handler = new CancelBookingCommandHandler(
            DbContext,
            currentUserServiceMock.Object,
            Mock.Of<IBookingNotificationPublisher>()
        );

        var command = new CancelBookingCommand(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
    }
}