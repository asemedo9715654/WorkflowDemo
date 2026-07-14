using Microsoft.Data.SqlClient;

namespace PedidoWorkflow.Data;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlExpress")
            ?? throw new InvalidOperationException("Connection string 'SqlExpress' nao configurada.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public SqlConnection CreateMasterConnection()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        };

        return new SqlConnection(builder.ConnectionString);
    }
}
