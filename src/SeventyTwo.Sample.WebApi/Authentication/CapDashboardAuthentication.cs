using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SeventyTwo.Sample.WebApi.Authentication;

/// <summary>
/// CAP Dashboard Basic Authentication 凭据配置。
/// </summary>
public sealed class CapDashboardAuthenticationConfiguration
{
    /// <summary>
    /// 登录用户名。
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// 登录密码。
    /// </summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// CAP Dashboard 认证与授权使用的固定名称。
/// </summary>
public static class CapDashboardAuthenticationDefaults
{
    /// <summary>
    /// 根据请求路径转发到 Dashboard Basic 或业务 JWT 的认证方案名称。
    /// </summary>
    public const string PathBasedScheme = "PathBasedAuthentication";

    /// <summary>
    /// Dashboard 授权策略名称，供 CAP Dashboard 配置按名称引用；
    /// 该策略要求使用下方的 <see cref="BasicScheme"/> 认证方案并建立有效用户身份。
    /// </summary>
    public const string Policy = "CapDashboardAuthenticationPolicy";

    /// <summary>
    /// Basic Authentication 认证方案名称，用于注册和选择
    /// <see cref="CapDashboardBasicAuthenticationHandler"/>；该值是 ASP.NET Core 内部的方案标识，
    /// 不是 Authorization 请求头中的 Basic 前缀。
    /// </summary>
    public const string BasicScheme = "CapDashboardBasic";

    /// <summary>
    /// CAP Dashboard 的所有请求（包括未附带 Dashboard 授权元数据的静态资源）使用 Basic；
    /// 其他请求仍使用业务 JWT，避免 Dashboard 凭据被用于业务接口。
    /// </summary>
    public static string SelectScheme(PathString requestPath)
    {
        return requestPath.StartsWithSegments("/cap") ? BasicScheme : BusinessJwtAuthenticationDefaults.Scheme;
    }
}

/// <summary>
/// 提供凭据的固定时间比较，降低根据比较耗时推断凭据的风险。
/// </summary>
public static class CapDashboardCredentialComparer
{
    /// <summary>
    /// 比较请求凭据与配置凭据是否相同。
    /// </summary>
    public static bool Equals(string actual, string expected)
    {
        // 将双方按相同编码转换为字节序列，再使用固定时间算法比较，
        // 避免普通字符串比较因首个不匹配位置不同而产生明显的耗时差异。
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expected)
        );
    }
}

/// <summary>
/// CAP Dashboard Basic Authentication 认证选项。
/// </summary>
public sealed class CapDashboardBasicAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// 期望的 Dashboard 登录用户名，由启动配置从
    /// <see cref="CapDashboardAuthenticationConfiguration"/> 注入。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 期望的 Dashboard 登录密码，由启动配置从
    /// <see cref="CapDashboardAuthenticationConfiguration"/> 注入。
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 使用 HTTP Basic Authentication 认证 CAP Dashboard。
/// </summary>
public sealed class CapDashboardBasicAuthenticationHandler(
    IOptionsMonitor<CapDashboardBasicAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<CapDashboardBasicAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>
    /// 解析并校验当前请求的 Basic 凭据；成功时创建用户身份，凭据错误时返回认证失败，
    /// 未提供 Basic 凭据时返回无认证结果并交由授权阶段处理。
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Authorization 请求头的标准格式为“Basic {Base64凭据}”。
        // 请求未携带该请求头、格式无法解析、认证类型不是 Basic 或参数为空时，
        // 当前方案不生成身份，后续由授权策略触发 401 质询。
        if (
            !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var authorization)
            || !string.Equals(authorization.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorization.Parameter)
        )
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string credential;
        try
        {
            // Basic 参数是“用户名:密码”的 UTF-8 字节经过 Base64 编码后的结果。
            credential = Encoding.UTF8.GetString(Convert.FromBase64String(authorization.Parameter));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("CAP Dashboard Basic 凭据格式无效"));
        }

        // 只使用第一个冒号作为用户名与密码的分隔符，因此密码自身可以继续包含冒号。
        var separatorIndex = credential.IndexOf(':');
        if (separatorIndex < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("CAP Dashboard Basic 凭据格式无效"));
        }

        var userName = credential[..separatorIndex];
        var password = credential[(separatorIndex + 1)..];

        // 启动时注入的用户名和密码必须均为非空值，请求凭据则通过固定时间算法比较，
        // 任一条件不满足都统一返回认证失败，避免向客户端透露具体失败字段。
        if (
            string.IsNullOrWhiteSpace(Options.UserName)
            || string.IsNullOrWhiteSpace(Options.Password)
            || !CapDashboardCredentialComparer.Equals(userName, Options.UserName)
            || !CapDashboardCredentialComparer.Equals(password, Options.Password)
        )
        {
            return Task.FromResult(AuthenticateResult.Fail("CAP Dashboard Basic 用户名或密码无效"));
        }

        // Basic 凭据校验通过后创建已认证身份；Dashboard 当前只需要用户名声明，
        // 不包含业务用户 ID、角色或业务会话信息。
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], Scheme.Name);
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name))
        );
    }

    /// <summary>
    /// 授权策略要求认证但当前请求未认证时发起质询，返回 401 并通过
    /// WWW-Authenticate 响应头通知客户端使用 Basic Authentication。
    /// </summary>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // 通知浏览器使用 Basic Authentication；生产环境必须通过 HTTPS 传输凭据。
        Response.Headers.WWWAuthenticate = "Basic realm=\"CAP Dashboard\"";
        await base.HandleChallengeAsync(properties);
    }
}
