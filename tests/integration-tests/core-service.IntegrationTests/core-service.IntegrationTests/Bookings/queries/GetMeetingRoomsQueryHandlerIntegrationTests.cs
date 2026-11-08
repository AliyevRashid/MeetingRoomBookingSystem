using common_library;
using core_service.Application.Interfaces;
using core_service.Application.Rooms.Queries;
using core_service.IntegrationTests;
using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Moq;
using Npgsql;

public class GetMeetingRoomsQueryHandlerIntegrationTests : PostgresIntegrationTestBase
{
    

    [Theory]
    [InlineData(1, 5, 3, 3)] 
    [InlineData(1, 2, 3, 2)] 
    [InlineData(2, 2, 3, 1)] 
    public async Task Handle_Should_ReturnPagedAndSortedRooms_When_OrganizationHasRooms(
        int pageNumber, int pageSize, int roomsToCreateCount, int expectedItemsCountOnPage)
    {
        // Arrange
        var organization = new OrganizationBuilder()
                            .WithName($"Rooms Org {Guid.NewGuid().ToString().Substring(0, 6)}")
                            .WithTaxId(new Random().Next(10000000, 99999999).ToString())
                            .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var roomNames = new[] { "Zeta Room", "Alpha Room", "Beta Room", "Gamma Room", "Delta Room" };

        for (int i = 0; i < roomsToCreateCount; i++)
        {
            var room = new MeetingRoomBuilder()
                .WithName(roomNames[i])
                .WithCapacity(10 + i)
                .WithOrganizationId(organization.Id)
                .Build();
            await DbContext.MeetingRooms.AddAsync(room);
        }
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new GetMeetingRoomsQueryHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new GetMeetingRoomsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(roomsToCreateCount);
        result.Value.PageNumber.Should().Be(pageNumber);
        result.Value.PageSize.Should().Be(pageSize);
        result.Value.Items.Should().HaveCount(expectedItemsCountOnPage);

        var itemNames = result.Value.Items.Select(r => r.Name).ToList();
        itemNames.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthenticatedOrHasNoOrganization()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns((Guid?)null);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new GetMeetingRoomsQueryHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new GetMeetingRoomsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
    }
}