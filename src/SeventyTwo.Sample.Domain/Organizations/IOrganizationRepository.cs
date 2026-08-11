namespace SeventyTwo.Sample.Domain.Organizations;

/// <summary>
/// 机构仓储。
/// </summary>
public interface IOrganizationRepository
{
    /// <summary>
    /// 在当前事务内串行化机构结构变更，防止并发创建、移动或删除破坏层级约束。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AcquireMutationLockAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 按 ID 查找未删除的机构。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的机构；不存在时返回 <see langword="null"/>。</returns>
    Task<Organization?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取所有未删除的机构。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>机构只读列表。</returns>
    Task<IReadOnlyList<Organization>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 检查同一机构树内是否存在指定编码。
    /// </summary>
    /// <param name="orgId">机构树根 ID。</param>
    /// <param name="code">机构编码。</param>
    /// <param name="excludedId">检查时排除的机构 ID；不排除时为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> CodeExistsAsync(Guid orgId, string code, Guid? excludedId, CancellationToken cancellationToken);

    /// <summary>
    /// 新增机构。
    /// </summary>
    /// <param name="organization">待新增的机构。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AddAsync(Organization organization, CancellationToken cancellationToken);

    /// <summary>
    /// 保存机构变更。
    /// </summary>
    /// <param name="organization">待保存的机构。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(Organization organization, CancellationToken cancellationToken);

    /// <summary>
    /// 删除指定机构。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
