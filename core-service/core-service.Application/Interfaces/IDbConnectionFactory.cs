using System.Data;

namespace core_service.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
