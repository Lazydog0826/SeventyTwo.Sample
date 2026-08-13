namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户应用服务。
/// </summary>
public interface IUserApplication
{
    /// <summary>
    /// 获取指定用户的编辑详情。
    /// </summary>
    Task<UserListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户只读列表。</returns>
    Task<IReadOnlyList<UserListOutput>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 创建用户。
    /// </summary>
    /// <param name="input">创建用户的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的用户信息。</returns>
    Task<UserListOutput> CreateAsync(CreateUserInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 更新指定用户。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="input">更新用户的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(Guid id, UpdateUserInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 设置指定用户的启用状态。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="input">用户启用状态设置输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SetEnableAsync(Guid id, SetUserEnableInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 为指定用户生成并设置新密码，同时使既有登录会话失效。
    /// </summary>
    Task<ResetPasswordOutput> ResetPasswordAsync(Guid id, Guid version, CancellationToken cancellationToken);

    /// <summary>
    /// 删除指定用户。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="version">客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户信息。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">用于取消用户信息查询的令牌。</param>
    /// <returns>用户信息。</returns>
    Task<UserOutput> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 用户登录。
    /// </summary>
    /// <param name="request">登录输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>登录令牌。</returns>
    Task<LoginOutput> LoginAsync(LoginInput request, CancellationToken cancellationToken);

    /// <summary>
    /// 使用刷新令牌轮换访问令牌和刷新令牌。
    /// </summary>
    /// <param name="refreshToken">刷新令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新的令牌。</returns>
    Task<LoginOutput> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// 退出当前登录会话。
    /// </summary>
    /// <param name="refreshToken">刷新令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}
