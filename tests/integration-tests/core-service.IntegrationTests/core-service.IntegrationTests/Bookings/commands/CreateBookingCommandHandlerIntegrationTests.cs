using common_library;
using core_service.Application.Bookings.Commands.CreateBooking;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.Infrastructure;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

public class CreateBookingCommandHandlerIntegrationTests : PostgresIntegrationTestBase
{

    [Fact]
    public async Task Handle_Should_CreateBookingInDatabase_When_RequestIsValid()
    {
        // Arrange
        var organization = new OrganizationBuilder()
            .WithName("Create Booking Org")
            .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1,"Engineering", organization.Id);
        await DbContext.Departments.AddAsync(department.Value);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder()
            .WithName("Main Conference Room")
            .WithOrganizationId(organization.Id)
            .Build();
        await DbContext.MeetingRooms.AddAsync(room);

        var author = new UserBuilder()
            .WithEmail("author@company.com")
            .WithOrganizationId(organization.Id)
            .Build();
        await DbContext.Users.AddAsync(author);

        var participant = new UserBuilder()
            .WithOrganizationId(organization.Id)
            .Build();
        await DbContext.Users.AddAsync(participant);

        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(author.Id);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

        var availabilityCheckerMock = new Mock<IBookingAvailabilityChecker>();
        availabilityCheckerMock
            .Setup(a => a.HasOverlapAsync(room.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var notificationPublisherMock = new Mock<IBookingNotificationPublisher>();

        var handler = new CreateBookingCommandHandler(
            availabilityCheckerMock.Object,
            DbContext,
            currentUserServiceMock.Object,
            notificationPublisherMock.Object
        );

        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);

        var command = new CreateBookingCommand(
            room.Id,
            startTime,
            endTime,
            new List<Guid> { participant.Id },
            "Architecture Review"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var bookingInDb = await DbContext.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == result.Value);

        bookingInDb.Should().NotBeNull();
        bookingInDb!.Subject.Should().Be("Architecture Review");
        bookingInDb.RoomId.Should().Be(room.Id);
        bookingInDb.AuthorId.Should().Be(author.Id);

        var participantsCount = await DbContext.BookingParticipants
            .CountAsync(bp => bp.BookingId == bookingInDb.Id);

        participantsCount.Should().Be(1);

        notificationPublisherMock.Verify(
            n => n.PublishNotificationAsync(
                It.IsAny<Booking>(),
                "Main Conference Room",
                "author@company.com",
                organization.Id,
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(participant.Id)),
                "Architecture Review",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task Handle_Should_ReturnConcurrencyConflict_When_TwoCommandsExecuteConcurrentlyForSameRoom()
    {
        // Arrange
        var organization = new OrganizationBuilder()
                                .WithName("Concurrency Org")
                                .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Engineering", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder()
                            .WithOrganizationId(organization.Id)
                            .Build();
        await DbContext.MeetingRooms.AddAsync(room);
        await DbContext.SaveChangesAsync();

        var author = new UserBuilder()
                            .WithEmail("author@company.com")
                            .WithOrganizationId(organization.Id)
                            .WithDepartmentId(department.Id)
                            .Build();
        await DbContext.Users.AddAsync(author);
        await DbContext.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<CoreServiceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        var availabilityCheckerMock = new Mock<IBookingAvailabilityChecker>();
        availabilityCheckerMock
            .Setup(a => a.HasOverlapAsync(room.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var notificationPublisherMock = new Mock<IBookingNotificationPublisher>();

        var startTime = DateTime.UtcNow.AddDays(1);
        var command1 = new CreateBookingCommand(room.Id, startTime, startTime.AddHours(1), new(), "Meeting 1");
        var command2 = new CreateBookingCommand(room.Id, startTime, startTime.AddHours(1), new(), "Meeting 2");

        // Act: Запускаем два параллельных потока, каждый со своим DbContext
        var task1 = Task.Run(async () =>
        {
            await using var dbContext = new CoreServiceDbContext(options);
            var userServiceMock = new Mock<ICurrentUserService>();
            userServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
            userServiceMock.Setup(s => s.UserId).Returns(author.Id);
            userServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

            var handler = new CreateBookingCommandHandler(
                availabilityCheckerMock.Object,
                dbContext,
                userServiceMock.Object,
                notificationPublisherMock.Object
            );
            return await handler.Handle(command1, CancellationToken.None);
        });

        var task2 = Task.Run(async () =>
        {
            await using var dbContext = new CoreServiceDbContext(options);
            var userServiceMock = new Mock<ICurrentUserService>();
            userServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
            userServiceMock.Setup(s => s.UserId).Returns(author.Id);
            userServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

            var handler = new CreateBookingCommandHandler(
                availabilityCheckerMock.Object,
                dbContext,
                userServiceMock.Object,
                notificationPublisherMock.Object
            );
            return await handler.Handle(command2, CancellationToken.None);
        });

        var results = await Task.WhenAll(task1, task2);

        results.Should().ContainSingle(r => r.IsSuccess);
        results.Should().ContainSingle(r => r.IsFailure 
                                         && r.ErrorCode == ErrorCodes.ConcurrencyConflict);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_RoomBelongsToAnotherOrganization()
    {
        // Arrange
        var orgA = new OrganizationBuilder().WithName("Org A").WithTaxId("11111111").Build();
        var orgB = new OrganizationBuilder().WithName("Org B").WithTaxId("22222222").Build();
        DbContext.Organizations.AddRange(orgA, orgB);
        await DbContext.SaveChangesAsync();

        var departmentB = Department.Create(2, "Dept B", orgB.Id).Value;
        DbContext.Departments.Add(departmentB);
        await DbContext.SaveChangesAsync();

        // Комната принадлежит Org B
        var roomInOrgB = new MeetingRoomBuilder()
            .WithOrganizationId(orgB.Id)
            .Build();
        DbContext.MeetingRooms.Add(roomInOrgB);

        // Пользователь принадлежит Org A
        var departmentA = Department.Create(1, "Dept A", orgA.Id).Value;
        DbContext.Departments.Add(departmentA);
        await DbContext.SaveChangesAsync();

        var authorFromOrgA = new UserBuilder()
            .WithEmail("author@orga.com")
            .WithOrganizationId(orgA.Id)
            .WithDepartmentId(departmentA.Id)
            .Build();
        DbContext.Users.Add(authorFromOrgA);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(authorFromOrgA.Id);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(orgA.Id); // Пользователь из Org A

        var handler = new CreateBookingCommandHandler(
            Mock.Of<IBookingAvailabilityChecker>(),
            DbContext,
            currentUserServiceMock.Object,
            Mock.Of<IBookingNotificationPublisher>()
        );

        // Пытаемся забронировать комнату из Org B, находясь в Org A
        var command = new CreateBookingCommand(
            roomInOrgB.Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            new(),
            "Cross-Org Meeting"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnWrongInputError_When_ParticipantBelongsToAnotherOrganization()
    {
        // Arrange
        var orgA = new OrganizationBuilder().WithName("Org A").WithTaxId("11111111").Build();
        var orgB = new OrganizationBuilder().WithName("Org B").WithTaxId("22222222").Build();
        DbContext.Organizations.AddRange(orgA, orgB);
        await DbContext.SaveChangesAsync();

        var departmentA = Department.Create(1, "Dept A", orgA.Id).Value;
        var departmentB = Department.Create(2, "Dept B", orgB.Id).Value;
        DbContext.Departments.AddRange(departmentA, departmentB);
        await DbContext.SaveChangesAsync();

        var roomInOrgA = new MeetingRoomBuilder()
            .WithOrganizationId(orgA.Id)
            .Build();
        DbContext.MeetingRooms.Add(roomInOrgA);

        var authorFromOrgA = new UserBuilder()
            .WithEmail("author@orga.com")
            .WithOrganizationId(orgA.Id)
            .WithDepartmentId(departmentA.Id)
            .Build();
        DbContext.Users.Add(authorFromOrgA);

        // Участник принадлежит Org B
        var participantFromOrgB = new UserBuilder()
            .WithOrganizationId(orgB.Id)
            .WithDepartmentId(departmentB.Id)
            .Build();
        DbContext.Users.Add(participantFromOrgB);

        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(authorFromOrgA.Id);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(orgA.Id);

        var handler = new CreateBookingCommandHandler(
            Mock.Of<IBookingAvailabilityChecker>(),
            DbContext,
            currentUserServiceMock.Object,
            Mock.Of<IBookingNotificationPublisher>()
        );

        // Пытаемся добавить участника из чужой организации (Org B)
        var command = new CreateBookingCommand(
            roomInOrgA.Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            new List<Guid> { participantFromOrgB.Id },
            "Meeting with outsider"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.WrongInputError);
    }
}