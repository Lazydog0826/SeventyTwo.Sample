namespace SeventyTwo.Sample.Domain.Products;

public interface IProductRepository
{
    /// <summary>
    /// 根据 ID 查询未删除的商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品聚合；不存在时返回 <see langword="null"/>。</returns>
    Task<Product?> FindAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// 分页查询未删除的商品。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品分页数据。</returns>
    Task<ProductPage> GetPageAsync(PageRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 新增商品。
    /// </summary>
    /// <param name="product">商品聚合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AddAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// 保存商品修改或软删除状态。
    /// </summary>
    /// <param name="product">商品聚合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(Product product, CancellationToken cancellationToken);
}

public sealed record ProductPage(IReadOnlyCollection<Product> Items, int Total);
