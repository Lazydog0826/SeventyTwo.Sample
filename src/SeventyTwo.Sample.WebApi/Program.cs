using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Infrastructure.Authentication;
using SeventyTwo.Sample.Infrastructure.Messaging;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Infrastructure;
using ApplicationAssemblyMarker = SeventyTwo.Sample.Application.AssemblyMarker;
using DomainAssemblyMarker = SeventyTwo.Sample.Domain.AssemblyMarker;
using InfrastructureAssemblyMarker = SeventyTwo.Sample.Infrastructure.AssemblyMarker;
using WebApiAssemblyMarker = SeventyTwo.Sample.WebApi.AssemblyMarker;

#region 应用程序集

// 声明当前应用涉及的程序集，供依赖注入、对象映射、CAP 订阅者等
// 基于程序集扫描的功能发现类型。
var appAssemblies = new[]
{
    typeof(WebApiAssemblyMarker).Assembly,
    typeof(ApplicationAssemblyMarker).Assembly,
    typeof(DomainAssemblyMarker).Assembly,
    typeof(InfrastructureAssemblyMarker).Assembly,
};
var appAssemblyList = appAssemblies.Distinct().ToList();
var appDomainTypes = appAssemblyList.GetTypeListByAssemblyList();

#endregion

#region 宿主初始化

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

#endregion

#region 日志

// 使用 Serilog 接管宿主日志：控制台仅记录 Information 级别，
// 文件仅记录 Error 及以上级别，并按天滚动，最多保留最近 30 个日志文件。
builder.Host.UseSerilog(
    (_, configuration) =>
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Logger(consoleConfiguration =>
                consoleConfiguration
                    .Filter.ByIncludingOnly(logEvent => logEvent.Level == LogEventLevel.Information)
                    .WriteTo.Console()
            )
            .WriteTo.File(
                "logs/log-.txt",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30
            )
);

#endregion

#region 依赖注入

// 将 ASP.NET Core 的默认依赖注入容器替换为 Autofac，并扫描应用域中已收集的类型，
// 自动注册符合 InfraKit 约定的依赖服务。
builder.Host.UseAutofac(containerBuilder => containerBuilder.AutoAddDependency(appDomainTypes));

#endregion

#region 跨域服务

var corsConfiguration = builder.Configuration.GetRequiredSection(nameof(CorsConfiguration)).Get<CorsConfiguration>()!;
builder.Services.AddAppCors(
    builder.Environment.IsDevelopment(),
    corsConfiguration.Origins,
    corsConfiguration.Headers,
    corsConfiguration.Methods
);

#endregion

#region 对象映射

// 扫描应用程序集中的 Mapster 映射配置，并注册到全局配置中。
TypeAdapterConfig.GlobalSettings.Scan([.. appAssemblyList]);

#endregion

#region 缓存

// 注册进程内存缓存以及 InfraKit 对缓存访问的统一封装。
builder.Services.AddMemoryCache();
builder.Services.AddCacheService(builder.Configuration);

#endregion

#region JWT 配置

// 将配置文件中的 JwtConfiguration 节绑定为强类型配置，供 JWT 令牌服务读取，
// 用于生成及校验令牌的签发者、受众、密钥和有效期等参数。
builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection(nameof(JwtConfiguration)));

#endregion

#region 异常处理

// 注册全局 API 异常处理器与标准 Problem Details 响应支持。
// 请求管道中的 UseExceptionHandler 会调用这里注册的处理器，将异常转换为统一错误响应。
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

#endregion

#region 权限服务

// 根据接口上的 PermissionAttribute 动态构建授权策略，并校验当前用户的权限编码。
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

#endregion

#region 持久化

// 注册数据库上下文、仓储等持久化基础设施。
builder.Services.AddPersistence(builder.Configuration);

#endregion

#region 控制器

// 注册控制器，并应用项目统一的 JSON 序列化配置。
builder.Services.AddControllers().AddJsonOptions(JsonConfiguration.Configure);

#endregion

#region 模型验证

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

#endregion

#region CAP 配置

// CAP 的数据库、RabbitMQ 配置以及 Dashboard 登录凭据均为必需配置；
// 缺少配置节时立即终止启动，避免应用在消息基础设施不完整的状态下运行。
var capConfiguration = builder.Configuration.GetRequiredSection(nameof(CapConfiguration)).Get<CapConfiguration>()!;
var postgreSqlConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrWhiteSpace(postgreSqlConnectionString))
{
    throw new InvalidOperationException("未配置 ConnectionStrings:PostgreSQL");
}

var dashboardAuthenticationConfiguration = builder
    .Configuration.GetRequiredSection(nameof(CapDashboardAuthenticationConfiguration))
    .Get<CapDashboardAuthenticationConfiguration>()!;

#endregion

#region 认证授权

// 注册授权与认证方案。未显式指定授权策略的业务接口统一使用业务 JWT；
// CAP Dashboard 则使用独立的 Basic Authentication，避免管理端凭据与业务令牌混用。
builder
    .Services.AddAuthorization(options =>
    {
        // 兜底授权策略按请求路径选择认证方案：CAP Dashboard（包括静态资源）使用 Basic，
        // 其他未显式放行的端点仍使用业务 JWT。
        options.FallbackPolicy = new AuthorizationPolicyBuilder(CapDashboardAuthenticationDefaults.PathBasedScheme)
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
        // 默认认证与质询使用业务 JWT；CAP 请求由显式策略或兜底路径策略选择 Basic。
        options.DefaultAuthenticateScheme = BusinessJwtAuthenticationDefaults.Scheme;
        options.DefaultChallengeScheme = BusinessJwtAuthenticationDefaults.Scheme;
    })
    .AddPolicyScheme(
        CapDashboardAuthenticationDefaults.PathBasedScheme,
        displayName: null,
        options =>
        {
            options.ForwardDefaultSelector = context =>
                CapDashboardAuthenticationDefaults.SelectScheme(context.Request.Path);
        }
    )
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

#endregion

#region CAP

// 注册 CAP 分布式事务消息组件。PostgreSQL 用于保存消息及事务状态，
// RabbitMQ 用作消息传输，Dashboard 用于查看消息处理情况。
builder
    .Services.AddCap(x =>
    {
        // 消费者方法总共尝试三次；达到阈值后原消息保留为 CAP Failed 状态。
        x.FailedRetryCount = 3;

        // 禁止匿名访问 Dashboard，并将请求交由上方的 Dashboard 专用授权策略校验。
        x.UseDashboard(options =>
        {
            options.AllowAnonymousExplicit = false;
            options.AuthorizationPolicy = CapDashboardAuthenticationDefaults.Policy;
        });

        // CAP 使用 PostgreSQL 进行消息持久化，并与业务数据库事务协同。
        x.UsePostgreSql(postgreSqlConnectionString);

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

#region OpenAPI

builder.Services.AddOpenApi(
    "v1",
    options =>
    {
        options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
    }
);

#endregion

#region 构建应用

var app = builder.Build();

#endregion

#region 全局异常处理中间件

// 全局异常处理中间件必须位于业务端点之前，才能捕获后续管道抛出的异常。
app.UseExceptionHandler();

#endregion

#region 静态文件中间件

// 优先响应 wwwroot 中的静态文件；未命中时继续进入后续请求管道。
app.UseStaticFiles();

#endregion

#region 路由中间件

// 根据已注册的端点匹配当前请求，为后续认证、授权及端点执行提供路由信息。
app.UseRouting();

#endregion

#region 跨域

app.UseCors();

#endregion

#region 认证授权中间件

// 认证必须先于授权执行：先建立当前用户身份，再根据授权策略判断访问权限。
app.UseAuthentication();
app.UseAuthorization();

#endregion

#region 控制器端点

// 将特性路由控制器映射为可执行端点。
app.MapControllers();

#endregion

#region 基础设施端点

// 以下路径由基础设施保留，业务端点不得重复映射。
app.MapHealthChecks("/health").AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.yaml").AllowAnonymous();
}

#endregion

#region 运行应用

await app.RunAsync();

#endregion
