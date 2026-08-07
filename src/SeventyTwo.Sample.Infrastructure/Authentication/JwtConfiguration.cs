// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Infrastructure.Authentication;

/// <summary>
/// JWT 签名、加密及有效期配置。
/// </summary>
public sealed class JwtConfiguration
{
    /// <summary>
    /// 获取令牌签发者。
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// 获取令牌接收者。
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// 获取用于 HS256 签名的 UTF-8 密钥。
    /// </summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// 获取用于 A256GCM 加密的 Base64 密钥，解码后必须为 32 字节。
    /// </summary>
    public string EncryptionKey { get; init; } = string.Empty;

    /// <summary>
    /// 获取 Access Token 有效时长，单位为分钟。
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; }

    /// <summary>
    /// 获取 Refresh Token 有效时长，单位为天。
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; }
}
