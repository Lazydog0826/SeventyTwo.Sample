namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 权限应用服务。
/// </summary>
public interface IPermissionApplication
{
    /// <summary>
    /// 获取指定权限的编辑详情。
    /// </summary>
    Task<PermissionListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 创建权限并发布权限列表缓存失效消息。
    /// </summary>
    Task<PermissionListOutput> CreateAsync(CreatePermissionInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 修改权限并发布权限列表缓存失效消息。
    /// </summary>
    Task UpdateAsync(Guid id, UpdatePermissionInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 物理删除权限及用户权限关联，并发布权限列表缓存失效消息。
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取权限管理列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有未删除权限。</returns>
    Task<IReadOnlyList<PermissionListOutput>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取可配置为用户默认页面的有效页面权限。
    /// </summary>
    Task<IReadOnlyList<DefaultPageOptionOutput>> GetDefaultPageOptionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户权限。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户权限。</returns>
    Task<PermissionOutput> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户授权编辑数据，包含全部权限及用户当前关联的权限 ID。
    /// </summary>
    /// <param name="userId">目标用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户授权编辑数据。</returns>
    Task<UserAuthorizationOutput> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 整体保存用户授权，允许保留已禁用权限的关联。
    /// </summary>
    /// <param name="userId">目标用户 ID。</param>
    /// <param name="permissionIds">完整权限 ID 集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AuthorizeAsync(Guid userId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken);
}
