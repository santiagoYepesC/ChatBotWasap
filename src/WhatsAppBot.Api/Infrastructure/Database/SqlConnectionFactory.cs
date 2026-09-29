using Microsoft.Data.SqlClient;

namespace WhatsAppBot.Api.Infrastructure.Database;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}

public sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public SqlConnection CreateConnection() => new(connectionString);
}
