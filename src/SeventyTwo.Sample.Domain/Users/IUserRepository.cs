namespace SeventyTwo.Sample.Domain.Users;

/// <summary>
/// 用户仓储。
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根据用户 ID 查询用户。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">用于取消查询的令牌。</param>
    /// <returns>用户聚合；不存在时返回 <see langword="null"/>。</returns>
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 根据账号查询用户。
    /// </summary>
    /// <param name="account">用户账号。</param>
    /// <param name="cancellationToken">用于取消查询的令牌。</param>
    /// <returns>用户聚合；不存在时返回 <see langword="null"/>。</returns>
    Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken);
}
