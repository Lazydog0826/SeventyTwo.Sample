using Mapster;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Infrastructure.Messaging;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Infrastructure;
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
        builder.Host.UseSerilog(
            (_, configuration) =>
                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
        );
        builder.Host.UseAutofac(containerBuilder => containerBuilder.AutoAddDependency(HostApp.AppDomainTypes));
        TypeAdapterConfig.GlobalSettings.Scan([.. HostApp.AppAssemblyList]);
        builder.Services.AddMemoryCache();
        builder.Services.AddCacheService();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddControllers().AddJsonOptions(JsonConfiguration.Configure);
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var message = string.Join(
                    "；",
                    context.ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage)
                );
                throw new ApiValidationException(message);
            };
        });

        #region CAP

        var capConfiguration = builder
            .Configuration.GetRequiredSection(nameof(CapConfiguration))
            .Get<CapConfiguration>()!;
        var dashboardAuthenticationConfiguration = builder
            .Configuration.GetRequiredSection(nameof(CapDashboardAuthenticationConfiguration))
            .Get<CapDashboardAuthenticationConfiguration>()!;

        // 注册仅允许通过 CAP Dashboard Basic Authentication 的授权策略。
        builder
            .Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    CapDashboardAuthenticationDefaults.Policy,
                    policy =>
                        policy
                            .AddAuthenticationSchemes(CapDashboardAuthenticationDefaults.BasicScheme)
                            .RequireAuthenticatedUser()
                );
            })
            .AddAuthentication()
            .AddScheme<CapDashboardBasicAuthenticationOptions, CapDashboardBasicAuthenticationHandler>(
                CapDashboardAuthenticationDefaults.BasicScheme,
                options =>
                {
                    options.UserName = dashboardAuthenticationConfiguration.UserName;
                    options.Password = dashboardAuthenticationConfiguration.Password;
                }
            );
        builder
            .Services.AddCap(x =>
            {
                // 禁止匿名访问，并将 Dashboard 请求交由上方授权策略校验。
                x.UseDashboard(options =>
                {
                    options.AllowAnonymousExplicit = false;
                    options.AuthorizationPolicy = CapDashboardAuthenticationDefaults.Policy;
                });
                x.UsePostgreSql(capConfiguration.PostgreSqlConnectionString);
                x.UseRabbitMQ(options =>
                {
                    options.HostName = capConfiguration.RabbitMqHostName;
                    options.UserName = capConfiguration.RabbitMqUserName;
                    options.Password = capConfiguration.RabbitMqPassword;
                    options.VirtualHost = capConfiguration.RabbitMqVirtualHost;
                    options.PublishConfirms = true;
                    options.QueueOptions.Durable = true;
                });
            })
            .AddSubscriberAssembly(typeof(InfrastructureAssemblyMarker));

        #endregion

        return Task.CompletedTask;
    },
    app =>
    {
        app.UseExceptionHandler();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return Task.CompletedTask;
    }
);
