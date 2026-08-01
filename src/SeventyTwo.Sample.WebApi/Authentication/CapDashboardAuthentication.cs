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
    /// Dashboard 授权策略名称。
    /// </summary>
    public const string Policy = "CapDashboardAuthenticationPolicy";

    /// <summary>
    /// Basic Authentication 认证方案名称。
    /// </summary>
    public const string BasicScheme = "CapDashboardBasic";
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
    /// 期望的登录用户名。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 期望的登录密码。
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
        // 请求未携带合法的 Basic Authorization 头时不生成身份，由授权策略触发质询。
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
            // Basic 凭据格式为 Base64("用户名:密码")，因此仅按第一个冒号拆分。
            credential = Encoding.UTF8.GetString(Convert.FromBase64String(authorization.Parameter));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("CAP Dashboard Basic 凭据格式无效"));
        }

        var separatorIndex = credential.IndexOf(':');
        if (separatorIndex < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("CAP Dashboard Basic 凭据格式无效"));
        }

        var userName = credential[..separatorIndex];
        var password = credential[(separatorIndex + 1)..];
        if (
            string.IsNullOrWhiteSpace(Options.UserName)
            || string.IsNullOrWhiteSpace(Options.Password)
            || !CapDashboardCredentialComparer.Equals(userName, Options.UserName)
            || !CapDashboardCredentialComparer.Equals(password, Options.Password)
        )
        {
            return Task.FromResult(AuthenticateResult.Fail("CAP Dashboard Basic 用户名或密码无效"));
        }

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
