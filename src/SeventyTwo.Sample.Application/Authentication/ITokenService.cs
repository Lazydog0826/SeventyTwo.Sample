using SeventyTwo.Sample.Domain.Users;

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
