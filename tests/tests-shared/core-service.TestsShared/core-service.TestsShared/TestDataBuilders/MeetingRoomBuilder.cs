using Bogus;
using core_service.Domain.Entities;

namespace core_service.UnitTests.TestDataBuilders;

public class MeetingRoomBuilder
{
    private string _name = new Faker().Commerce.Department();
    private int _capacity = 10;
    private Guid _organizationId = Guid.NewGuid();

    public MeetingRoomBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public MeetingRoomBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        return this;
    }

    public MeetingRoomBuilder WithOrganizationId(Guid organizationId)
    {
        _organizationId = organizationId;
        return this;
    }

    public MeetingRoom Build()
    {
        var result = MeetingRoom.Create(_name, _capacity, _organizationId);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to build MeetingRoom: {result.ErrorCode}");
        }
        return result.Value;
    }
}