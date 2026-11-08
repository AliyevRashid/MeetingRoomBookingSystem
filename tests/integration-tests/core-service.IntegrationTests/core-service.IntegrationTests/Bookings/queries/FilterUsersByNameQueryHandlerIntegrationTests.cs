using common_library;
using core_service.Application.Interfaces;
using core_service.Application.Users.Queries;
using core_service.Domain.Entities;
using core_service.UnitTests.TestDataBuilders;
using Moq;
using FluentAssertions;

namespace core_service.IntegrationTests.Bookings.queries;

public class FilterUsersByNameQueryHandlerIntegrationTests : PostgresIntegrationTestBase
{
    [Theory]
    [InlineData(null, 2)]     
    [InlineData("Ali", 1)]    
    [InlineData("John", 1)]  
    [InlineData("xyz", 0)]   
    [InlineData("Al", 2)]  
    public async Task Handle_Should_FilterUsersCorrectly_When_QueryIsExecuted(string? searchName, int expectedCount)
    {
        // Arrange
        var organization = new OrganizationBuilder()
                                .WithName($"User Filter Org {Guid.NewGuid().ToString().Substring(0, 6)}")
                                .WithTaxId(new Random().Next(10000000, 99999999).ToString())
                                .Build();
        await DbContext.Organizations.AddAsync(organization);
        await DbContext.SaveChangesAsync();

        var department = Department.Create(1, "Engineering", organization.Id).Value;
        await DbContext.Departments.AddAsync(department);
        await DbContext.SaveChangesAsync();

        var user1 = new UserBuilder()
                        .WithEmail("ali@company.com")
                        .WithFullName("Ali Aliyev")
                        .WithOrganizationId(organization.Id)
                        .WithDepartmentId(department.Id)
                        .Build();

        var user2 = new UserBuilder()
                        .WithEmail("john@company.com")
                        .WithFullName("John Doe")
                        .WithOrganizationId(organization.Id)
                        .WithDepartmentId(department.Id)
                        .Build();

        await DbContext.Users.AddRangeAsync(user1, user2);
        await DbContext.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns(organization.Id);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new FilterUsersByNameQueryHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new FilterUsersByNameQuery(pageNumber: 1, pageSize: 10, userName: searchName);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(expectedCount);
        result.Value.Items.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_When_UserIsNotAuthenticatedOrHasNoOrganization()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserServiceMock.Setup(s => s.OrganizationId).Returns((Guid?)null);

        var connectionFactory = new TestDbConnectionFactory(ConnectionString);
        var handler = new FilterUsersByNameQueryHandler(connectionFactory, currentUserServiceMock.Object);

        var query = new FilterUsersByNameQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.Unauthorized);
    }
}
