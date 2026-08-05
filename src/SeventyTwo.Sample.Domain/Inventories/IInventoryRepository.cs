namespace SeventyTwo.Sample.Domain.Inventories;

public interface IInventoryRepository
{
    /// <summary>
    /// 登记库存变更请求，用于保证业务请求幂等。
    /// </summary>
    /// <param name="requestNo">业务请求号。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>首次登记返回 <see langword="true"/>；请求已登记返回 <see langword="false"/>。</returns>
    Task<bool> TryRegisterChangeAsync(string requestNo, CancellationToken cancellationToken);

    /// <summary>
    /// 确保指定库存维度对应的变更锁记录存在。
    /// </summary>
    /// <param name="keys">库存维度集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task EnsureChangeLocksAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);

    /// <summary>
    /// 获取指定库存维度的变更锁，并查询可用库存。
    /// </summary>
    /// <param name="keys">库存维度集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可用于库存变更的库存集合。</returns>
    Task<IReadOnlyList<Inventory>> GetForChangeAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 保存库存新增、库存修改和库存变更明细。
    /// </summary>
    /// <param name="requestNo">业务请求号。</param>
    /// <param name="newInventories">新增的库存集合。</param>
    /// <param name="changedInventories">已修改的库存集合。</param>
    /// <param name="changes">库存变更明细集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveChangeAsync(
        string requestNo,
        IReadOnlyCollection<Inventory> newInventories,
        IReadOnlyCollection<Inventory> changedInventories,
        IReadOnlyCollection<InventoryChange> changes,
        CancellationToken cancellationToken
    );
}
