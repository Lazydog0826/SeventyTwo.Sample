using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Persistence;

public static class InfrastructureSetup
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("未配置 ConnectionStrings:DefaultDb");
        }

        services.AddScoped<ISqlSugarClient>(
            _ =>
                new SqlSugarClient(
                    new ConnectionConfig
                    {
                        DbType = DbType.PostgreSQL,
                        IsAutoCloseConnection = true,
                        ConnectionString = connectionString,
                    }
                )
        );
        return services;
    }
}
