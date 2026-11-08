using Bogus;
using core_service.Domain.Entities;

namespace core_service.UnitTests.TestDataBuilders;

public class OrganizationBuilder
{
    private string _name = new Faker().Company.CompanyName();
    private string _taxId = "12345678"; 

    public OrganizationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public OrganizationBuilder WithTaxId(string taxId)
    {
        _taxId = taxId;
        return this;
    }

    public Organization Build()
    {
        var result = Organization.Create(_name, _taxId);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to build Organization: {result.ErrorCode}");
        }
        return result.Value;
    }
}