using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Shared.Infrastructure.Data;

public class DapperDbContext
{
    private readonly string _connectionString;

    public DapperDbContext(IOptions<ConnectionStrings> connectionStrings)
    {
        _connectionString = connectionStrings.Value.Postgres ??
                            throw new NullReferenceException(nameof(connectionStrings));
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
