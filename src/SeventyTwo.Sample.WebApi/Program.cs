using SeventyTwo.InfraKit.ApiLog;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.InfraKit.Core.App;
using SeventyTwo.InfraKit.Core.App.JsonConverter;
using SeventyTwo.InfraKit.SnowFlake;
using SeventyTwo.Sample.Application.Abstractions;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Infrastructure.Ids;
using SeventyTwo.Sample.Infrastructure.Orders;
using SeventyTwo.Sample.Infrastructure.Persistence;
using ApiLogSetup = SeventyTwo.InfraKit.ApiLog.Setup;

await HostApp.StartWebAppAsync(
    args,
    [
        typeof(Program).Assembly,
        typeof(IIdGenerator).Assembly,
        typeof(Order).Assembly,
        typeof(SnowflakeIdGenerator).Assembly,
    ],
    builder =>
    {
        builder.Host.UseAutofac(containerBuilder => containerBuilder.AutoAddDependency(HostApp.AppDomainTypes));
        builder.Services.AddApiLog();
        builder.Services.Configure<RecordLogEvent>(options => options.Event += ApiLogSetup.WriteLogFile);
        builder.Services.AddCacheService();
        builder.Services.AddTransaction();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddScoped<IOrderApplication, OrderApplication>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IIdGenerator, SnowflakeIdGenerator>();
        builder
            .Services.AddControllers(options =>
            {
                options.Filters.Add<CoreActionFilter>();
                options.Filters.Add<RecordRequestFilter>();
            })
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
            .AddJsonOptions(JsonConfiguration.Configure);

        builder.Services.Configure<SnowFlakeConfiguration>(
            builder.Configuration.GetSection(nameof(SnowFlakeConfiguration))
        );
        builder.Services.AddHostedService<SnowFlakeHostService>();
        return Task.CompletedTask;
    },
    app =>
    {
        app.UseInfraKitExceptionHandler((_, _) => Task.FromResult(WebApiResponse.Error("服务异常")));
        app.UseRouting();
        app.MapControllers();
        return Task.CompletedTask;
    }
);
