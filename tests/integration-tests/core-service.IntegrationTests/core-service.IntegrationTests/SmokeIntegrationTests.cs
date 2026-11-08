using core_service.UnitTests.TestDataBuilders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class SmokeIntegrationTests : PostgresIntegrationTestBase
{
    [Fact]
    public async Task CanConnectAndSaveEntityToDatabase()
    {
        // Arrange
        var organization = new OrganizationBuilder()
                                .WithName("Smoke Test Org")
                                .Build();

        DbContext.Organizations.Add(organization);
        await DbContext.SaveChangesAsync();

        var room = new MeetingRoomBuilder()
                            .WithName("Test Room")
                            .WithCapacity(15)
                            .WithOrganizationId(organization.Id)
                            .Build();

        // Act
        DbContext.MeetingRooms.Add(room);
        await DbContext.SaveChangesAsync();

        // Assert
        var roomFromDb = await DbContext.MeetingRooms
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(r => r.Id == room.Id);

        roomFromDb.Should().NotBeNull();
        roomFromDb!.Name.Should().Be("Test Room");
        roomFromDb.OrganizationId.Should().Be(organization.Id);
    }
}