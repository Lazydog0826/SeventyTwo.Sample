using System.Net;
using SeventyTwo.InfraKit.ApiLog;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.InfraKit.Core.App;
using SeventyTwo.InfraKit.Core.App.JsonConverter;
using SeventyTwo.InfraKit.SnowFlake;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Domain.Inventories;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.Infrastructure.Persistence;
using ApiLogSetup = SeventyTwo.InfraKit.ApiLog.Setup;

await HostApp.StartWebAppAsync(
    args,
    [
        typeof(Program).Assembly,
        typeof(OrderApplication).Assembly,
        typeof(Order).Assembly,
        typeof(InfrastructureSetup).Assembly,
    ],
    builder =>
    {
        builder.Host.UseAutofac(containerBuilder => containerBuilder.AutoAddDependency(HostApp.AppDomainTypes));
        builder.Services.AddApiLog();
        builder.Services.Configure<RecordLogEvent>(options => options.Event += ApiLogSetup.WriteLogFile);
        builder.Services.AddCacheService();
        builder.Services.AddPersistence(builder.Configuration);
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
        app.UseInfraKitExceptionHandler(
            (_, exception) =>
            {
                var isDomainException =
                    exception is OrderDomainException or InventoryDomainException or ProductDomainException;
                var response = isDomainException
                    ? WebApiResponse.Error(exception.Message, HttpStatusCode.BadRequest)
                    : WebApiResponse.Error("服务异常");
                return Task.FromResult((response, SaveLog: !isDomainException));
            }
        );
        app.UseRouting();
        app.MapControllers();
        return Task.CompletedTask;
    }
);
