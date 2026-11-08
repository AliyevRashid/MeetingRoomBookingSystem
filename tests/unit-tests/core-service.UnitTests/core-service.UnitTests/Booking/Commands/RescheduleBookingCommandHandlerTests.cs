using common_library;
using core_service.Application.Bookings.Commands.RescheduleBooking;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.Domain.Enums;
using core_service.UnitTests.Helpers;
using core_service.UnitTests.TestBuilders;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

public class RescheduleBookingCommandHandlerTests
{
    private readonly Mock<ICoreServiceDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IBookingNotificationPublisher> _notificationPublisherMock;
    private readonly Mock<IBookingAvailabilityChecker> _availabilityCheckerMock;
    private readonly RescheduleBookingCommandHandler _handler;

    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RescheduleBookingCommandHandlerTests()
    {
        _contextMock = new Mock<ICoreServiceDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _notificationPublisherMock = new Mock<IBookingNotificationPublisher>();
        _availabilityCheckerMock = new Mock<IBookingAvailabilityChecker>();

        _handler = new RescheduleBookingCommandHandler(
            _availabilityCheckerMock.Object,
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _notificationPublisherMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_RescheduleBooking_When_DataIsValidAndUserIsAuthor()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        var author = new UserBuilder().WithOrganizationId(_organizationId).Build();

        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, _userId);

        var booking = new BookingBuilder()
                            .WithId(bookingId)
                            .WithRoom(room)
                            .WithAuthor(author)
                            .WithTime(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2))
                            .Build();

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Default);

        var bookingsList = new List<Booking> { booking };
        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(bookingsList);
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var participantsList = new List<BookingParticipant>();
        var mockParticipantsDbSet = DbSetMockHelper.GetQueryableMockDbSet(participantsList);
        _contextMock.Setup(c => c.BookingParticipants).Returns(mockParticipantsDbSet.Object);

        var newStartDate = DateTime.UtcNow.AddDays(2);
        var newEndDate = newStartDate.AddHours(3);

        var command = new RescheduleBookingCommand(bookingId, newStartDate, newEndDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.StartTime.Should().Be(newStartDate);
        booking.EndTime.Should().Be(newEndDate);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _notificationPublisherMock.Verify(
                                    n => n.PublishNotificationAsync(
                                        It.IsAny<Booking>(),
                                        It.IsAny<string>(),
                                        It.IsAny<string>(),
                                        _organizationId,
                                        It.IsAny<IEnumerable<Guid>>(),
                                        It.IsAny<string>(),
                                        It.IsAny<CancellationToken>()),
                                    Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_TimeSlotIsInvalid()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        var author = new UserBuilder().WithOrganizationId(_organizationId).Build();
        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, _userId);

        var booking = new BookingBuilder()
            .WithId(bookingId)
            .WithRoom(room)
            .WithAuthor(author)
            .Build();

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);

        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<Booking> { booking });
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var invalidStartDate = DateTime.UtcNow.AddDays(2);
        var invalidEndDate = invalidStartDate.AddHours(-1);

        var command = new RescheduleBookingCommand(bookingId, invalidStartDate, invalidEndDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);

        var command = new RescheduleBookingCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_BookingDoesNotExistOrNotInOrganization()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);

        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<Booking>());
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var command = new RescheduleBookingCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthorAndNotAdmin()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        var author = new UserBuilder().WithOrganizationId(_organizationId).Build();
        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, anotherUserId);

        var booking = new BookingBuilder()
            .WithId(bookingId)
            .WithRoom(room)
            .WithAuthor(author)
            .Build();

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId); 
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Default); 

        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<Booking> { booking });
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var command = new RescheduleBookingCommand(bookingId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_RescheduleBooking_When_UserIsAdmin_EvenIfNotAuthor()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        var author = new UserBuilder().WithOrganizationId(_organizationId).Build();
        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, anotherUserId);

        var booking = new BookingBuilder()
            .WithId(bookingId)
            .WithRoom(room)
            .WithAuthor(author)
            .WithTime(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2))
            .Build();

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Admin); // Пользователь — Админ

        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<Booking> { booking });
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var mockParticipantsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<BookingParticipant>());
        _contextMock.Setup(c => c.BookingParticipants).Returns(mockParticipantsDbSet.Object);

        var newStartDate = DateTime.UtcNow.AddDays(3);
        var newEndDate = newStartDate.AddHours(2);

        var command = new RescheduleBookingCommand(bookingId, newStartDate, newEndDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.StartTime.Should().Be(newStartDate);
        booking.EndTime.Should().Be(newEndDate);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_When_TimeSlotHasOverlap()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        var author = new UserBuilder().WithOrganizationId(_organizationId).Build();
        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, _userId);

        var booking = new BookingBuilder()
            .WithId(bookingId)
            .WithRoom(room)
            .WithAuthor(author)
            .Build();

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Default);

        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<Booking> { booking });
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var mockParticipantsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<BookingParticipant>());
        _contextMock.Setup(c => c.BookingParticipants).Returns(mockParticipantsDbSet.Object);

        _availabilityCheckerMock
            .Setup(a => a.HasOverlapAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); 

        var command = new RescheduleBookingCommand(bookingId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Conflict);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConcurrencyConflict_When_DbUpdateConcurrencyExceptionOccurs()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        var author = new UserBuilder().WithOrganizationId(_organizationId).Build();
        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, _userId);

        var booking = new BookingBuilder()
            .WithId(bookingId)
            .WithRoom(room)
            .WithAuthor(author)
            .Build();

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Default);

        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<Booking> { booking });
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var mockParticipantsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<BookingParticipant>());
        _contextMock.Setup(c => c.BookingParticipants).Returns(mockParticipantsDbSet.Object);

        // Настраиваем SaveChangesAsync так, чтобы он выбрасывал DbUpdateConcurrencyException
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var command = new RescheduleBookingCommand(bookingId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.ConcurrencyConflict);
    }
}