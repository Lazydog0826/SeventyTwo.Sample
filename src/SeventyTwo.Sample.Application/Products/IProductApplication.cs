using SeventyTwo.InfraKit.ShareDto;

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
    /// 修改商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="input">修改商品输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(long id, UpdateProductInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 软删除商品。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 查询商品详情。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品信息。</returns>
    Task<ProductOutput> GetAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 分页查询商品。
    /// </summary>
    /// <param name="request">分页及动态表达式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品分页数据。</returns>
    Task<PageResponse<ProductOutput>> GetPageAsync(PageRequest request, CancellationToken cancellationToken);
}
