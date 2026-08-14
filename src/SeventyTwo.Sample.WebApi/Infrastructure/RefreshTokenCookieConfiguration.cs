using Microsoft.AspNetCore.Http;

namespace SeventyTwo.Sample.WebApi.Infrastructure;

/// <summary>
/// 刷新令牌 Cookie 配置。
/// </summary>
public sealed class RefreshTokenCookieConfiguration
{
    /// <summary>
    /// Cookie 的 SameSite 策略，仅允许 Lax 或 Strict，以阻止跨站请求携带刷新令牌。
    /// </summary>
    public SameSiteMode SameSite { get; init; } = SameSiteMode.Lax;
}
