using common_library;
using core_service.Application.Bookings.Commands.CreateBooking;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.UnitTests.Helpers;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Moq;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<ICoreServiceDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IBookingNotificationPublisher> _notificationPublisherMock;
    private readonly Mock<IBookingAvailabilityChecker> _availabilityCheckerMock;
    private readonly CreateBookingCommandHandler _handler;

    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _roomId = Guid.NewGuid();

    public CreateBookingCommandHandlerTests()
    {
        _contextMock = new Mock<ICoreServiceDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _notificationPublisherMock = new Mock<IBookingNotificationPublisher>();
        _availabilityCheckerMock = new Mock<IBookingAvailabilityChecker>();

        _handler = new CreateBookingCommandHandler(
            _availabilityCheckerMock.Object,
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _notificationPublisherMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_CreateBooking_When_DataIsValid()
    {
        // Arrange
        var room = new MeetingRoomBuilder().WithName("Conference Room").WithOrganizationId(_organizationId).Build();
        typeof(MeetingRoom).GetProperty(nameof(MeetingRoom.Id))?.SetValue(room, _roomId);

        var author = new UserBuilder().WithEmail("author@test.com").WithOrganizationId(_organizationId).Build();
        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, _userId);

        var participantId = Guid.NewGuid();

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);

        var roomsList = new List<MeetingRoom> { room };
        var mockRoomsDbSet = DbSetMockHelper.GetQueryableMockDbSet(roomsList);
        _contextMock.Setup(c => c.MeetingRooms).Returns(mockRoomsDbSet.Object);

        var usersList = new List<User>
        {
            author,
            new UserBuilder().WithOrganizationId(_organizationId).Build() 
        };

        typeof(User).GetProperty(nameof(User.Id))?.SetValue(usersList[1], participantId);

        var mockUsersDbSet = DbSetMockHelper.GetQueryableMockDbSet(usersList);
        _contextMock.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

        var bookingsList = new List<Booking>();
        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(bookingsList);
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        var participantsList = new List<BookingParticipant>();
        var mockParticipantsDbSet = DbSetMockHelper.GetQueryableMockDbSet(participantsList);
        _contextMock.Setup(c => c.BookingParticipants).Returns(mockParticipantsDbSet.Object);

        _availabilityCheckerMock
            .Setup(a => a.HasOverlapAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);

        var command = new CreateBookingCommand(
            _roomId,
            startTime,
            endTime,
            new List<Guid> { participantId },
            "Team Sync"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationPublisherMock.Verify(
            n => n.PublishNotificationAsync(
                It.IsAny<Booking>(),
                "Conference Room",
                "author@test.com",
                _organizationId,
                It.IsAny<IEnumerable<Guid>>(),
                "Team Sync",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);

        var command = new CreateBookingCommand(_roomId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), new(), "Meeting");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_RoomDoesNotExistOrNotInOrganization()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);

        var mockRoomsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<MeetingRoom>());
        _contextMock.Setup(c => c.MeetingRooms).Returns(mockRoomsDbSet.Object);

        var command = new CreateBookingCommand(_roomId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), new(), "Meeting");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnWrongInputError_When_ParticipantsAreInvalid()
    {
        // Arrange
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        typeof(MeetingRoom).GetProperty(nameof(MeetingRoom.Id))?.SetValue(room, _roomId);

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);

        var mockRoomsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<MeetingRoom> { room });
        _contextMock.Setup(c => c.MeetingRooms).Returns(mockRoomsDbSet.Object);

        var mockUsersDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<User>());
        _contextMock.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

        var command = new CreateBookingCommand(
            _roomId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            new List<Guid> { Guid.NewGuid() }, 
            "Meeting"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.WrongInputError);
    }

    [Fact]
    public async Task Handle_Should_ReturnConcurrencyConflict_When_TimeSlotHasOverlap()
    {
        // Arrange
        var room = new MeetingRoomBuilder().WithOrganizationId(_organizationId).Build();
        typeof(MeetingRoom).GetProperty(nameof(MeetingRoom.Id))?.SetValue(room, _roomId);

        var author = new UserBuilder().WithEmail("author@test.com").WithOrganizationId(_organizationId).Build();
        typeof(User).GetProperty(nameof(User.Id))?.SetValue(author, _userId);

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(_organizationId);

        var mockRoomsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<MeetingRoom> { room });
        _contextMock.Setup(c => c.MeetingRooms).Returns(mockRoomsDbSet.Object);

        var mockUsersDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<User> { author });
        _contextMock.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

        var mockBookingsDbSet = DbSetMockHelper.GetQueryableMockDbSet(new List<Booking>());
        _contextMock.Setup(c => c.Bookings).Returns(mockBookingsDbSet.Object);

        _availabilityCheckerMock
            .Setup(a => a.HasOverlapAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateBookingCommand(
            _roomId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            new(),
            "Meeting"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.ConcurrencyConflict);
    }
}