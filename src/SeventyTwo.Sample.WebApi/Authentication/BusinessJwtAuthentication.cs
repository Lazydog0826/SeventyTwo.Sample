using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Authentication;

namespace SeventyTwo.Sample.WebApi.Authentication;

/// <summary>
/// 业务 JWT 认证使用的固定名称。
/// </summary>
public static class BusinessJwtAuthenticationDefaults
{
    /// <summary>
    /// 业务 JWT 认证方案名称，用于注册和选择 <see cref="BusinessJwtAuthenticationHandler"/>；
    /// 该值是 ASP.NET Core 内部的方案标识，不是 Authorization 请求头中的 Bearer 前缀。
    /// </summary>
    public const string Scheme = "BusinessBearer";
}

/// <summary>
/// 业务 JWT 认证方案选项。
/// 当前没有额外配置项，保留该类型以满足自定义认证方案的注册约定及后续扩展需要。
/// </summary>
public sealed class BusinessJwtAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>
/// 业务 JWT 认证处理器：从 Authorization 请求头读取 Bearer Token，
/// 校验令牌内容及 Redis 会话状态，并在成功后创建当前请求的用户身份。
/// </summary>
public sealed class BusinessJwtAuthenticationHandler(
    IOptionsMonitor<BusinessJwtAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenService tokenService,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : AuthenticationHandler<BusinessJwtAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>
    /// 执行业务访问令牌认证。
    /// 请求未提供 Bearer Token 时返回无认证结果；令牌或会话校验失败时返回认证失败；
    /// 全部校验通过后返回包含业务用户声明的认证票据。
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Authorization 请求头的标准格式为“Bearer {token}”。
        // 请求头不存在、格式无法解析、认证类型不是 Bearer 或 Token 为空时，
        // 当前方案不建立用户身份，后续由授权阶段决定是否发起 401 质询。
        if (
            !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var authorization)
            || !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorization.Parameter)
        )
        {
            return AuthenticateResult.NoResult();
        }

        // 先完成 Token 的签名、有效期及载荷解析校验，并限制这里只接受访问令牌；
        // 刷新令牌等其他类型的 Token 不能用来访问业务接口。
        var token = authorization.Parameter;
        if (!tokenService.TryValidate(token, out var payload) || payload is not { TokenType: "access" })
        {
            return AuthenticateResult.Fail("访问令牌无效");
        }

        // 根据 Token 中的会话 ID 生成 Redis Key，并一次读取当前会话保存的
        // Token 哈希和用户 ID，用于确认该 Token 仍属于有效登录会话。
        var cacheKey = cacheConfiguration.Value.Data("token-cache-key", payload.SessionId.ToString());
        var session = await redisCacheService.GetDatabase().HashGetAsync(cacheKey, ["accessTokenHash", "userId"]);

        // 除了 Token 本身的密码学有效性，还必须满足服务端会话校验：
        // 会话字段完整、访问令牌哈希一致、用户 ID 一致。退出登录、会话过期或 Token 被替换后，
        // 即使 Token 尚未超过自身有效期，也会在这里被判定为已失效。
        if (
            session.Length != 2
            || !string.Equals(session[0].ToString(), GetTokenHash(token), StringComparison.Ordinal)
            || !string.Equals(session[1].ToString(), payload.UserId.ToString(), StringComparison.Ordinal)
        )
        {
            return AuthenticateResult.Fail("访问令牌已失效");
        }

        // 将 Token 载荷转换为 ASP.NET Core ClaimsIdentity，供控制器、授权策略及业务代码
        // 通过 HttpContext.User 获取用户、显示名称和登录会话等信息。
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
                new Claim(ClaimTypes.Name, payload.Username),
                new Claim("display_name", payload.DisplayName),
                new Claim("session_id", payload.SessionId.ToString()),
            ],
            Scheme.Name
        );

        // 认证票据使用当前处理器实际运行的方案名，确保身份与业务 JWT 方案关联。
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    /// <summary>
    /// 授权要求用户已认证但当前请求未建立有效身份时发起质询，
    /// 返回 401，并通过 WWW-Authenticate 响应头提示客户端使用 Bearer Token。
    /// </summary>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        await base.HandleChallengeAsync(properties);
    }

    /// <summary>
    /// 使用 SHA-256 计算访问令牌摘要并编码为 Base64，避免在 Redis 会话中保存和比较令牌明文。
    /// </summary>
    private static string GetTokenHash(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
