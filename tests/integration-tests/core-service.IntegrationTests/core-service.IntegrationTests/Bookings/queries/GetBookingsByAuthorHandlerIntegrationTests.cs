using common_library;
using core_service.Application.Bookings.Queries;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.IntegrationTests;
using core_service.UnitTests.TestBuilders;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Moq;
using Npgsql;

public class GetBookingsByAuthorHandlerIntegrationTests : PostgresIntegrationTestBase
{
    
    [Fact]
    public async Task Handle_Should_ReturnPagedBookings_When_AuthorHasBookings()
    {
        // Arrange
        var organization = new OrganizationBuilder()
                                .WithName("Dapper Test Org")
                                .WithTaxId("99887766")
                                .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Engineering", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder()
                            .WithName("Boardroom A")
                            .WithOrganizationId(organization.Id)
                            .Build();
        DbContext.MeetingRooms.Add(room);

        var author = new UserBuilder()
                            .WithEmail("author@dapper.com")
                            .WithOrganizationId(organization.Id)
                            .WithDepartmentId(department.Id)
                            .Build();
        await DbContext.Users.AddAsync(author);
        await DbContext.SaveChangesAsync();

        var booking1 = new BookingBuilder()
                            .WithRoom(room)
                            .WithAuthor(author)
                            .WithSubject("Sprint Planning")
                            .WithTime(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1))
                            .Build();

        var booking2 = new BookingBuilder()
                            .WithRoom(room)
                            .WithAuthor(author)
                            .WithSubject("Retrospective")
                            .WithTime(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(1))
                            .Build();

        await DbContext.Bookings.AddRangeAsync(booking1, booking2);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(author.Id);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new GetBookingsByAuthorHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new GetBookingsByAuthorQuery(pageNumber: 1, pageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);

        var firstItem = result.Value.Items.First();
        firstItem.Subject.Should().Be("Retrospective");
        firstItem.RoomName.Should().Be("Boardroom A");
        firstItem.RoomId.Should().Be(room.Id);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new GetBookingsByAuthorHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new GetBookingsByAuthorQuery(pageNumber: 1, pageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
    }
}