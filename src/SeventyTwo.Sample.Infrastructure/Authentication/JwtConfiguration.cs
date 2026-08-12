// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Infrastructure.Authentication;

/// <summary>
/// JWT 签名和加密配置。
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
    /// 获取用于 A256CBC-HS512 加密的 Base64 密钥，解码后必须为 64 字节。
    /// </summary>
    public string EncryptionKey { get; init; } = string.Empty;
}
