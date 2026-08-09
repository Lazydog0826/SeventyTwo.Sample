namespace SeventyTwo.Sample.Domain.Permissions;

/// <summary>
/// 权限仓储。
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// 根据 ID 获取未删除权限。
    /// </summary>
    /// <returns>权限不存在时返回 <see langword="null"/>。</returns>
    Task<Permission?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 判断权限编码是否已被其他权限占用。
    /// </summary>
    /// <param name="excludedId">检查修改场景时需要排除的权限 ID。</param>
    Task<bool> CodeExistsAsync(string code, Guid? excludedId, CancellationToken cancellationToken);

    /// <summary>
    /// 判断权限是否存在未删除的直接下级。
    /// </summary>
    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取关联了指定权限的用户 ID，用于清除权限缓存。
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUserIdsAsync(Guid permissionId, CancellationToken cancellationToken);

    /// <summary>
    /// 新增权限。
    /// </summary>
    Task AddAsync(Permission permission, CancellationToken cancellationToken);

    /// <summary>
    /// 使用乐观锁保存权限修改。
    /// </summary>
    Task SaveAsync(Permission permission, CancellationToken cancellationToken);

    /// <summary>
    /// 在事务中物理删除权限及其用户权限关联。
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取所有未删除权限，包含已禁用权限。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有未删除权限。</returns>
    Task<IReadOnlyList<Permission>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取所有有效权限。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有有效权限。</returns>
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户拥有的权限编码。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户拥有的权限编码。</returns>
    Task<IReadOnlyList<string>> GetCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
