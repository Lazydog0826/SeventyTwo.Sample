namespace SeventyTwo.Sample.Domain.Users;

/// <summary>
/// 用户仓储。
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 在当前事务内获取指定用户的安全操作锁，串行化登录、启禁用与删除操作。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AcquireSecurityLockAsync(Guid id, CancellationToken cancellationToken);

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

    /// <summary>
    /// 获取所有未删除的用户。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户只读列表。</returns>
    Task<IReadOnlyList<User>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 检查是否存在指定用户名的未删除用户。
    /// </summary>
    /// <param name="username">用户名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// 新增用户。
    /// </summary>
    /// <param name="user">待新增的用户。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// 使用乐观锁保存用户变更。
    /// </summary>
    /// <param name="user">待保存的用户。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// 使用乐观锁保存密码摘要变更。
    /// </summary>
    Task SavePasswordAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// 使用乐观锁删除指定用户。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="version">客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken);
}
