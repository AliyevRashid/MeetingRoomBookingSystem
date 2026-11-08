using common_library;
using core_service.Application.Interfaces;
using core_service.Application.Rooms.Queries;
using core_service.Domain.Entities;
using core_service.IntegrationTests;
using core_service.UnitTests.TestBuilders;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Moq;
using Npgsql;

public class GetMeetingRoomByIdQueryHandlerIntegrationTests : PostgresIntegrationTestBase
{
    
    [Theory]
    [InlineData(1, 5, 2, 2)] 
    [InlineData(1, 1, 2, 1)] 
    [InlineData(2, 1, 2, 1)] 
    [InlineData(1, 5, 0, 0)] 
    public async Task Handle_Should_ReturnCorrectPagedBookings_When_QueryingRoomWithDifferentPagination(
        int pageNumber, int pageSize, int totalBookingsToCreate, int expectedItemsCountOnPage)
    {
        // Arrange
        var organization = new OrganizationBuilder()
            .WithName($"Room Query Org {Guid.NewGuid().ToString().Substring(0, 6)}")
            .WithTaxId(new Random().Next(10000000, 99999999).ToString())
            .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Engineering", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder()
                        .WithName("Innovation Lab")
                        .WithCapacity(25)
                        .WithOrganizationId(organization.Id)
                        .Build();
        await DbContext.MeetingRooms.AddAsync(room);

        var author = new UserBuilder()
                        .WithEmail($"author_{Guid.NewGuid().ToString().Substring(0, 6)}@room.com")
                        .WithOrganizationId(organization.Id)
                        .WithDepartmentId(department.Id)
                        .Build();
        await DbContext.Users.AddAsync(author);
        await DbContext.SaveChangesAsync();

        for (int i = 0; i < totalBookingsToCreate; i++)
        {
            var booking = new BookingBuilder()
                .WithRoom(room)
                .WithAuthor(author)
                .WithSubject($"Meeting {i + 1}")
                .WithTime(DateTime.UtcNow.AddDays(i + 1), DateTime.UtcNow.AddDays(i + 1).AddHours(1))
                .Build();
            await DbContext.Bookings.AddAsync(booking);
        }
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new GetMeetingRoomByIdQueryHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new GetMeetingRoomByIdQuery(room.Id, pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(room.Id);
        result.Value.Name.Should().Be("Innovation Lab");
        result.Value.Capacity.Should().Be(25);
        result.Value.OrganizationId.Should().Be(organization.Id);

        result.Value.Bookings.TotalCount.Should().Be(totalBookingsToCreate);
        result.Value.Bookings.PageNumber.Should().Be(pageNumber);
        result.Value.Bookings.PageSize.Should().Be(pageSize);
        result.Value.Bookings.Items.Should().HaveCount(expectedItemsCountOnPage);

        if (expectedItemsCountOnPage > 0)
        {
            var firstItem = result.Value.Bookings.Items.First();
            firstItem.Id.Should().NotBeEmpty();
            firstItem.AuthorName.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_RoomDoesNotExistOrBelongsToAnotherOrg()
    {
        // Arrange
        var orgA = new OrganizationBuilder().WithName("Org A").WithTaxId("55667788").Build();
        var orgB = new OrganizationBuilder().WithName("Org B").WithTaxId("99001122").Build();
        await DbContext.Organizations.AddRangeAsync(orgA, orgB);
        await DbContext.SaveChangesAsync();

        var roomInOrgB = new MeetingRoomBuilder()
            .WithOrganizationId(orgB.Id)
            .Build();
        await DbContext.MeetingRooms.AddAsync(roomInOrgB);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(orgA.Id);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new GetMeetingRoomByIdQueryHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new GetMeetingRoomByIdQuery(roomInOrgB.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthenticatedOrHasNoOrganization()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns((Guid?)null);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new GetMeetingRoomByIdQueryHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new GetMeetingRoomByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
    }
}