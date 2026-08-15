// ReSharper disable NotAccessedPositionalProperty.Global
using SeventyTwo.Sample.Domain.Users;

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
/// 数据权限类型与机构 ID 属于低频变更的身份属性，随令牌快照分发；
/// 用户资料变更会强制失效在途令牌，重新登录签发的令牌即携带新值。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="Username">用户名。</param>
/// <param name="DisplayName">用户显示名称。</param>
/// <param name="OrgId">用户所属机构 ID。</param>
/// <param name="DataPermissionType">数据权限类型。</param>
/// <param name="TokenType">令牌类型。</param>
/// <param name="SessionId">登录会话 ID。</param>
/// <param name="IssuedAtUnixTimeSeconds">令牌颁发时间的 UTC Unix 秒时间戳。</param>
public sealed record TokenPayload(
    Guid UserId,
    string Username,
    string DisplayName,
    Guid OrgId,
    DataPermissionType DataPermissionType,
    string TokenType,
    Guid SessionId,
    long IssuedAtUnixTimeSeconds
);
