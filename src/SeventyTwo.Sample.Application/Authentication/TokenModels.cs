// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.Application.Authentication;

/// <summary>
/// 用户访问令牌和刷新令牌。
/// </summary>
/// <param name="AccessToken">访问令牌。</param>
/// <param name="RefreshToken">刷新令牌。</param>
/// <param name="ExpireTime"></param>
public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime ExpireTime);

/// <summary>
/// JWT 中的用户和令牌类型数据。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="Username">用户名。</param>
/// <param name="DisplayName">用户显示名称。</param>
/// <param name="TokenType">令牌类型。</param>
/// <param name="SessionId">登录会话 ID。</param>
/// <param name="IssuedAtUnixTimeSeconds">令牌颁发时间的 UTC Unix 秒时间戳。</param>
public sealed record TokenPayload(
    Guid UserId,
    string Username,
    string DisplayName,
    string TokenType,
    Guid SessionId,
    long IssuedAtUnixTimeSeconds
);
