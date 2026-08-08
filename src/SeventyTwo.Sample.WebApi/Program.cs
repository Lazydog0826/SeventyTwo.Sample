using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Infrastructure.Authentication;
using SeventyTwo.Sample.Infrastructure.Messaging;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Infrastructure;
using ApplicationAssemblyMarker = SeventyTwo.Sample.Application.AssemblyMarker;
using DomainAssemblyMarker = SeventyTwo.Sample.Domain.AssemblyMarker;
using InfrastructureAssemblyMarker = SeventyTwo.Sample.Infrastructure.AssemblyMarker;
using WebApiAssemblyMarker = SeventyTwo.Sample.WebApi.AssemblyMarker;

// 通过 InfraKit 提供的统一宿主入口启动 Web API。
// 该方法负责创建 WebApplicationBuilder、执行下方的服务注册回调、构建应用、
// 执行中间件配置回调并最终运行应用，因此这里需要等待整个应用生命周期结束。
await HostApp.StartWebAppAsync(
    args,
    // 声明当前应用涉及的程序集，供 InfraKit 以及后续依赖注入、对象映射、
    // CAP 订阅者等基于程序集扫描的功能发现类型。
    [
        typeof(WebApiAssemblyMarker).Assembly,
        typeof(ApplicationAssemblyMarker).Assembly,
        typeof(DomainAssemblyMarker).Assembly,
        typeof(InfrastructureAssemblyMarker).Assembly,
    ],
    builder =>
    {
        // 使用 Serilog 接管宿主日志：默认记录 Information 及以上级别；
        // 将 ASP.NET Core 框架日志提高到 Warning，减少请求管道产生的冗余日志；
        // 同时附加日志上下文，并按天写入文件，最多保留最近 30 个日志文件。
        builder.Host.UseSerilog(
            (_, configuration) =>
                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
        );

        // 将 ASP.NET Core 的默认依赖注入容器替换为 Autofac，并扫描应用域中已收集的类型，
        // 自动注册符合 InfraKit 约定的依赖服务。
        builder.Host.UseAutofac(containerBuilder => containerBuilder.AutoAddDependency(HostApp.AppDomainTypes));

        // 扫描应用程序集中的 Mapster 映射配置，并注册到全局配置中。
        TypeAdapterConfig.GlobalSettings.Scan([.. HostApp.AppAssemblyList]);

        // 注册进程内存缓存以及 InfraKit 对缓存访问的统一封装。
        builder.Services.AddMemoryCache();
        builder.Services.AddCacheService();

        // 将配置文件中的 JwtConfiguration 节绑定为强类型配置，供 JWT 令牌服务读取，
        // 用于生成及校验令牌的签发者、受众、密钥和有效期等参数。
        builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection(nameof(JwtConfiguration)));

        // 注册全局 API 异常处理器与标准 Problem Details 响应支持。
        // 请求管道中的 UseExceptionHandler 会调用这里注册的处理器，将异常转换为统一错误响应。
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddProblemDetails();

        // 根据接口上的 PermissionAttribute 动态构建授权策略，并校验当前用户的权限编码。
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // 注册数据库上下文、仓储等持久化基础设施。
        builder.Services.AddPersistence(builder.Configuration);

        // 注册控制器，并应用项目统一的 JSON 序列化配置。
        builder.Services.AddControllers().AddJsonOptions(JsonConfiguration.Configure);

        // 覆盖 MVC 默认的模型验证失败响应：汇总所有字段的验证错误，
        // 再抛出业务验证异常，由全局异常处理器生成统一格式的 API 错误响应。
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

        // CAP 的数据库、RabbitMQ 配置以及 Dashboard 登录凭据均为必需配置；
        // 缺少配置节时立即终止启动，避免应用在消息基础设施不完整的状态下运行。
        var capConfiguration = builder
            .Configuration.GetRequiredSection(nameof(CapConfiguration))
            .Get<CapConfiguration>()!;
        var dashboardAuthenticationConfiguration = builder
            .Configuration.GetRequiredSection(nameof(CapDashboardAuthenticationConfiguration))
            .Get<CapDashboardAuthenticationConfiguration>()!;

        // 注册授权与认证方案。未显式指定授权策略的业务接口统一使用业务 JWT；
        // CAP Dashboard 则使用独立的 Basic Authentication，避免管理端凭据与业务令牌混用。
        builder
            .Services.AddAuthorization(options =>
            {
                // 兜底授权策略要求所有未显式放行的端点通过业务 JWT 完成身份认证。
                options.FallbackPolicy = new AuthorizationPolicyBuilder(BusinessJwtAuthenticationDefaults.Scheme)
                    .RequireAuthenticatedUser()
                    .Build();

                // Dashboard 专用策略仅接受其 Basic Authentication 方案。
                options.AddPolicy(
                    CapDashboardAuthenticationDefaults.Policy,
                    policy =>
                        policy
                            .AddAuthenticationSchemes(CapDashboardAuthenticationDefaults.BasicScheme)
                            .RequireAuthenticatedUser()
                );
            })
            .AddAuthentication(options =>
            {
                // 默认认证与质询均交给业务 JWT 方案处理。
                options.DefaultAuthenticateScheme = BusinessJwtAuthenticationDefaults.Scheme;
                options.DefaultChallengeScheme = BusinessJwtAuthenticationDefaults.Scheme;
            })
            // 注册业务 JWT 自定义认证处理器；处理器通过 ITokenService 校验令牌，
            // JwtConfiguration 由 ITokenService 的实现 JwtTokenService 读取。
            .AddScheme<BusinessJwtAuthenticationOptions, BusinessJwtAuthenticationHandler>(
                BusinessJwtAuthenticationDefaults.Scheme,
                _ => { }
            )
            // 注册 Dashboard Basic Authentication 处理器，并注入配置文件中的用户名与密码。
            .AddScheme<CapDashboardBasicAuthenticationOptions, CapDashboardBasicAuthenticationHandler>(
                CapDashboardAuthenticationDefaults.BasicScheme,
                options =>
                {
                    options.UserName = dashboardAuthenticationConfiguration.UserName;
                    options.Password = dashboardAuthenticationConfiguration.Password;
                }
            );

        #region CAP

        // 注册 CAP 分布式事务消息组件。PostgreSQL 用于保存消息及事务状态，
        // RabbitMQ 用作消息传输，Dashboard 用于查看消息处理情况。
        builder
            .Services.AddCap(x =>
            {
                // 禁止匿名访问 Dashboard，并将请求交由上方的 Dashboard 专用授权策略校验。
                x.UseDashboard(options =>
                {
                    options.AllowAnonymousExplicit = false;
                    options.AuthorizationPolicy = CapDashboardAuthenticationDefaults.Policy;
                });

                // CAP 使用 PostgreSQL 进行消息持久化，并与业务数据库事务协同。
                x.UsePostgreSql(capConfiguration.PostgreSqlConnectionString);

                // 配置 RabbitMQ 连接及可靠性选项：发布确认保证 Broker 已接收消息，
                // 持久化队列可在 RabbitMQ 重启后继续保留。
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
            // 扫描基础设施程序集，发现并注册其中实现的 CAP 消息订阅者。
            .AddSubscriberAssembly(typeof(InfrastructureAssemblyMarker));

        #endregion

        return Task.CompletedTask;
    },
    app =>
    {
        // 全局异常处理中间件必须位于业务端点之前，才能捕获后续管道抛出的异常。
        app.UseExceptionHandler();

        // 优先响应 wwwroot 中的静态文件；未命中时继续进入后续请求管道。
        app.UseStaticFiles();

        // 根据已注册的端点匹配当前请求，为后续认证、授权及端点执行提供路由信息。
        app.UseRouting();

        // 认证必须先于授权执行：先建立当前用户身份，再根据授权策略判断访问权限。
        app.UseAuthentication();
        app.UseAuthorization();

        // 将特性路由控制器映射为可执行端点。
        app.MapControllers();
        return Task.CompletedTask;
    }
);
