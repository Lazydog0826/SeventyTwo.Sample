namespace SeventyTwo.Sample.Domain.Products;

public interface IProductRepository
{
    /// <summary>
    /// 根据 ID 查询未删除的商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品聚合；不存在时返回 <see langword="null"/>。</returns>
    Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 分页查询未删除的商品。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品分页数据。</returns>
    Task<ProductPage> GetPageAsync(ProductPageRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 判断商品编码是否已被其他未删除商品占用。
    /// </summary>
    /// <param name="code">商品编码。</param>
    /// <param name="excludeId">需要排除的商品 ID，用于修改场景；新增时为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>编码已被占用时返回 <see langword="true"/>。</returns>
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);

    /// <summary>
    /// 新增商品。
    /// </summary>
    /// <param name="product">商品聚合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AddAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// 保存商品修改。
    /// </summary>
    /// <param name="product">商品聚合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// 物理删除商品，按乐观锁版本匹配删除行。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="version">客户端持有的商品版本 UUIDv7。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken);
}

public sealed record ProductPage(IReadOnlyCollection<Product> Items, int Total);

/// <summary>
/// 商品分页请求。
/// </summary>
public sealed class ProductPageRequest : PageRequest
{
    /// <summary>
    /// 关键字，匹配商品名称或编码。
    /// </summary>
    public string? Keyword { get; init; }

    /// <summary>
    /// 上架状态筛选；为 null 时不过滤。
    /// </summary>
    public ProductStatus? Status { get; init; }
}
