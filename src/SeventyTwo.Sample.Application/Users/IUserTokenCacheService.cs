using SeventyTwo.Sample.Application.Authentication;

namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户令牌会话缓存服务。
/// </summary>
public interface IUserTokenCacheService
{
    /// <summary>
    /// 保存用户登录会话。
    /// </summary>
    Task<bool> SaveAsync(Guid userId, Guid sessionId, TokenPair tokens, CancellationToken cancellationToken);

    /// <summary>
    /// 校验当前刷新令牌并轮换会话中的令牌。
    /// </summary>
    Task<bool> RefreshAsync(
        Guid userId,
        Guid sessionId,
        string refreshToken,
        TokenPair tokens,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 校验当前刷新令牌并删除用户登录会话。
    /// </summary>
    Task<bool> DeleteAsync(Guid userId, Guid sessionId, string refreshToken, CancellationToken cancellationToken);
}
