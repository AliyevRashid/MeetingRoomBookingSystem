using common_library;
using core_service.Application.Bookings.Commands.CancelBooking;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.Domain.Enums;
using core_service.UnitTests.Helpers;
using core_service.UnitTests.TestBuilders;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

public class CancelBookingCommandHandlerTests
{
    private readonly Mock<ICoreServiceDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IBookingNotificationPublisher> _notificationPublisherMock;
    private readonly CancelBookingCommandHandler _handler;

    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public CancelBookingCommandHandlerTests()
    {
        _contextMock = new Mock<ICoreServiceDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _notificationPublisherMock = new Mock<IBookingNotificationPublisher>();

        _handler = new CancelBookingCommandHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _notificationPublisherMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_CancelBooking_When_UserIsAuthor()
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
            .WithSubject("Team Sync")
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

        var command = new CancelBookingCommand(bookingId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        bookingsList.Should().NotContain(booking); 

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationPublisherMock.Verify(
            n => n.PublishNotificationAsync(
                booking,
                room.Name,
                author.Email,
                _organizationId,
                It.IsAny<IEnumerable<Guid>>(),
                "Team Sync cancelled",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CancelBooking_When_UserIsAdmin_EvenIfNotAuthor()
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
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRoles.Admin); 

        var bookingsList = new List<Booking> { booking };
        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(bookingsList);
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var mockParticipantsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<BookingParticipant>());
        _contextMock.Setup(c => c.BookingParticipants).Returns(mockParticipantsDbSet.Object);

        var command = new CancelBookingCommand(bookingId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        bookingsList.Should().NotContain(booking);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);

        var command = new CancelBookingCommand(Guid.NewGuid());

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

        var command = new CancelBookingCommand(Guid.NewGuid());

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

        var command = new CancelBookingCommand(bookingId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
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

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var command = new CancelBookingCommand(bookingId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.ConcurrencyConflict);
    }
}