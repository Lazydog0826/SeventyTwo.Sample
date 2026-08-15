using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeventyTwo.Sample.Application.Authentication;
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

        services.AddScoped<ISqlSugarClient>(sp =>
        {
            var client = new SqlSugarClient(
                new ConnectionConfig
                {
                    DbType = DbType.PostgreSQL,
                    IsAutoCloseConnection = true,
                    ConnectionString = connectionString,
                }
            );

            // 从同一作用域解析业务用户上下文并挂接公共字段自动填充；未注册时解析失败，启动即暴露配置缺失。
            CommonFieldInterceptor.Attach(client, sp.GetRequiredService<IBusinessUserContext>());
            return client;
        });
        return services;
    }
}
