using System.Data;
using Npgsql;

namespace infrastructure.Database;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IDbConnection> NewConnection(CancellationToken token = default)
    {
        var con = new NpgsqlConnection(_connectionString);
        await con.OpenAsync(token);
        return con;
    }
}

public interface IDbConnectionFactory
{
    Task<IDbConnection> NewConnection(CancellationToken token = default);
}