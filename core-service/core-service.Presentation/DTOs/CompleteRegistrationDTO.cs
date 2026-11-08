namespace core_service.Presentation.DTOs;

public record CompleteRegistrationRequest(string RegToken, string TaxId, int DepartmentId);