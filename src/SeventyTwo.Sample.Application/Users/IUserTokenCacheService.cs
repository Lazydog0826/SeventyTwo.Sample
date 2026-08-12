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
    /// 校验当前刷新令牌、用户失效分界时间及其事务快照，并轮换会话中的令牌。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="sessionId">登录会话 ID。</param>
    /// <param name="issuedAtUnixTimeSeconds">旧刷新令牌颁发时间的 UTC Unix 秒时间戳。</param>
    /// <param name="refreshToken">旧刷新令牌。</param>
    /// <param name="tokens">待写入的新令牌对。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>全部条件成立并完成轮换时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> RefreshAsync(
        Guid userId,
        Guid sessionId,
        long issuedAtUnixTimeSeconds,
        string refreshToken,
        TokenPair tokens,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 校验当前刷新令牌并删除用户登录会话。
    /// </summary>
    Task<bool> DeleteAsync(Guid userId, Guid sessionId, string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// 将指定用户当前时间之前颁发的令牌标记为失效，失效分界时间保留七天。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>设置成功时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> SetInvalidBeforeAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 判断令牌颁发时间是否晚于指定用户的令牌失效分界时间。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="issuedAtUnixTimeSeconds">令牌颁发时间的 UTC Unix 秒时间戳。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>
    /// 不存在失效分界时间或令牌颁发时间晚于失效分界时间时返回 <see langword="true"/>；
    /// 否则返回 <see langword="false"/>。
    /// </returns>
    Task<bool> IsTokenIssuedAfterInvalidBeforeAsync(
        Guid userId,
        long issuedAtUnixTimeSeconds,
        CancellationToken cancellationToken
    );
}
