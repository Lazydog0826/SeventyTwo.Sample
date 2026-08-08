using SeventyTwo.Sample.Domain.Users;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SeventyTwo.Sample.Application.Authentication;

/// <summary>
/// 定义用户访问令牌和刷新令牌的生成服务。
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 为指定用户生成访问令牌和刷新令牌。
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="sessionId"></param>
    /// <returns>令牌对。</returns>
    TokenPair Generate(User user, Guid sessionId);

    /// <summary>
    /// 验证令牌并获取令牌数据。
    /// </summary>
    /// <param name="token">令牌。</param>
    /// <param name="payload">验证成功时返回令牌数据，否则返回 <see langword="null" />。</param>
    /// <returns>令牌有效时返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    bool TryValidate(string token, out TokenPayload? payload);
}

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
public sealed record TokenPayload(Guid UserId, string Username, string DisplayName, string TokenType, Guid SessionId);
