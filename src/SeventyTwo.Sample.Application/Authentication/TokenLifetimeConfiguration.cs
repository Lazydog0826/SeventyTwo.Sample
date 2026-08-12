// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Application.Authentication;

/// <summary>
/// 令牌有效期配置。
/// </summary>
public sealed class TokenLifetimeConfiguration
{
    /// <summary>
    /// 获取 Access Token 有效时长，单位为分钟。
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; }

    /// <summary>
    /// 获取 Refresh Token 有效时长，单位为天。
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; }
}
