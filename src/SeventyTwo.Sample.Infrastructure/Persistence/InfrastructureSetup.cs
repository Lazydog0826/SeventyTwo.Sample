using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Persistence;

public static class InfrastructureSetup
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseType = configuration["ConnectionStrings:Type"];
        var dbType = databaseType?.ToLowerInvariant() switch
        {
            "postgresql" => DbType.PostgreSQL,
            "mysql" => DbType.MySql,
            "sqlserver" => DbType.SqlServer,
            _ => throw new InvalidOperationException("ConnectionStrings:Type 仅支持 PostgreSQL、MySql 或 SqlServer"),
        };

        var connectionString = configuration.GetConnectionString(databaseType);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"未配置 ConnectionStrings:{databaseType}");
        }

        services.AddScoped<ISqlSugarClient>(_ => new SqlSugarClient(
            new ConnectionConfig
            {
                DbType = dbType,
                IsAutoCloseConnection = true,
                ConnectionString = connectionString,
            }
        ));
        return services;
    }
}
