namespace SeventyTwo.Sample.Domain.Products;

/// <summary>
/// 商品类目仓储。
/// </summary>
public interface IProductCategoryRepository
{
    /// <summary>
    /// 获取类目树变更的事务级互斥锁，防止并发破坏层级路径。
    /// </summary>
    Task AcquireMutationLockAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 按主键查找未删除的类目。
    /// </summary>
    Task<ProductCategory?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取全部未删除的类目。
    /// </summary>
    Task<IReadOnlyList<ProductCategory>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 判断指定类目是否存在未删除的下级类目。
    /// </summary>
    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 新增类目。
    /// </summary>
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken);

    /// <summary>
    /// 保存类目变更；类目带有删除标记时执行软删除。以乐观锁版本作为更新条件。
    /// </summary>
    Task SaveAsync(ProductCategory category, CancellationToken cancellationToken);
}
