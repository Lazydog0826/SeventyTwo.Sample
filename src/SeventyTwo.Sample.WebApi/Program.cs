using System.Net;
using SeventyTwo.InfraKit.ApiLog;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.InfraKit.Core.App;
using SeventyTwo.InfraKit.Core.App.JsonConverter;
using SeventyTwo.InfraKit.SnowFlake;
using SeventyTwo.Sample.Domain.Inventories;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.Domain.Wallets;
using SeventyTwo.Sample.Infrastructure.Persistence;
using ApiLogSetup = SeventyTwo.InfraKit.ApiLog.Setup;
using ApplicationAssemblyMarker = SeventyTwo.Sample.Application.AssemblyMarker;
using DomainAssemblyMarker = SeventyTwo.Sample.Domain.AssemblyMarker;
using InfrastructureAssemblyMarker = SeventyTwo.Sample.Infrastructure.AssemblyMarker;
using WebApiAssemblyMarker = SeventyTwo.Sample.WebApi.AssemblyMarker;

await HostApp.StartWebAppAsync(
    args,
    [
        typeof(WebApiAssemblyMarker).Assembly,
        typeof(ApplicationAssemblyMarker).Assembly,
        typeof(DomainAssemblyMarker).Assembly,
        typeof(InfrastructureAssemblyMarker).Assembly,
    ],
    builder =>
    {
        builder.Host.UseAutofac(containerBuilder => containerBuilder.AutoAddDependency(HostApp.AppDomainTypes));
        builder.Services.AddApiLog();
        builder.Services.AddAutoMapper(_ => { }, HostApp.AppAssemblyList);
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
                if (exception is ProductNotFoundException)
                {
                    return Task.FromResult((WebApiResponse.Error(exception.Message), SaveLog: false));
                }

                var isDomainException =
                    exception
                    is InventoryDomainException
                        or OrderDomainException
                        or ProductDomainException
                        or WalletDomainException;
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
