using System.Data;
using Npgsql;

namespace DesafioArquiteturaSoftware.Query.Data;

public sealed class QueryDbConnectionFactory
{
    private readonly string _connectionString;

    public QueryDbConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("ReadDatabase")
            ?? throw new InvalidOperationException(
                "ReadDatabase connection string was not configured.");
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}