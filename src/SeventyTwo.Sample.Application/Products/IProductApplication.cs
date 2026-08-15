using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

public interface IProductApplication
{
    /// <summary>
    /// 创建商品。
    /// </summary>
    /// <param name="input">创建商品输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的商品信息。</returns>
    Task<ProductOutput> CreateAsync(CreateProductInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 修改商品，仅限当前用户数据权限范围内的商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="input">修改商品输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(Guid id, UpdateProductInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 切换商品上架状态，仅限当前用户数据权限范围内的商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="status">目标上架状态。</param>
    /// <param name="version">客户端持有的商品版本 UUIDv7。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ChangeStatusAsync(Guid id, ProductStatus status, Guid version, CancellationToken cancellationToken);

    /// <summary>
    /// 物理删除商品，仅限当前用户数据权限范围内的商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="version">客户端持有的商品版本 UUIDv7。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken);

    /// <summary>
    /// 查询商品详情，仅限当前用户数据权限范围内的商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品信息。</returns>
    Task<ProductOutput> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 分页查询商品，并按当前用户的数据权限过滤。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品分页数据。</returns>
    Task<PageResponse<ProductOutput>> GetPageAsync(ProductPageRequest request, CancellationToken cancellationToken);
}
