namespace SeventyTwo.Sample.Domain.Permissions;

/// <summary>
/// 权限仓储。
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// 在当前事务内获取权限目录共享锁，允许用户授权并发读取和校验权限树，
    /// 但与权限目录变更互斥。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AcquireCatalogSharedLockAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 在当前事务内获取权限目录独占锁，串行化权限新增、修改和删除，
    /// 并与用户授权的权限树读取和校验互斥。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AcquireCatalogMutationLockAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 根据 ID 获取未删除权限。
    /// </summary>
    /// <returns>权限不存在时返回 <see langword="null"/>。</returns>
    Task<Permission?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 判断权限编码是否已被其他权限占用。
    /// </summary>
    /// <param name="code"></param>
    /// <param name="excludedId">检查修改场景时需要排除的权限 ID。</param>
    /// <param name="cancellationToken"></param>
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
    /// 获取自身及完整祖先链均已启用的有效权限。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有有效权限。</returns>
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户拥有且自身及完整祖先链均已启用的权限编码。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户拥有的权限编码。</returns>
    Task<IReadOnlyList<string>> GetCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户当前关联的权限 ID，包含已禁用权限。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户当前关联的权限 ID。</returns>
    Task<IReadOnlyList<Guid>> GetIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 整体替换用户权限关联，事务边界由应用层管理。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="permissionIds">需要保存的完整权限 ID 集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ReplaceUserPermissionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken
    );
}
