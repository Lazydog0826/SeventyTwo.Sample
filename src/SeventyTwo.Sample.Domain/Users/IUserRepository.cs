namespace SeventyTwo.Sample.Domain.Users;

/// <summary>
/// 用户仓储。
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根据账号查询用户。
    /// </summary>
    /// <param name="account">用户账号。</param>
    /// <returns>用户聚合；不存在时返回 <see langword="null"/>。</returns>
    Task<User?> GetByAccountAsync(string account);
}
