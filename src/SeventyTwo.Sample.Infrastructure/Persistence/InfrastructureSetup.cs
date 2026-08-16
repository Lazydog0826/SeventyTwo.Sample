using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Persistence;

public static class InfrastructureSetup
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("未配置 ConnectionStrings:PostgreSQL");
        }

        services.AddScoped<ISqlSugarClient>(_ =>
        {
            var client = new SqlSugarClient(
                new ConnectionConfig
                {
                    DbType = DbType.PostgreSQL,
                    IsAutoCloseConnection = true,
                    ConnectionString = connectionString,
                }
            );

            return client;
        });
        return services;
    }
}
