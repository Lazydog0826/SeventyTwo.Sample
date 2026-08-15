namespace SeventyTwo.Sample.Application.Products;

/// <summary>
/// 商品类目应用服务。
/// </summary>
public interface IProductCategoryApplication
{
    /// <summary>
    /// 获取指定类目的编辑详情。
    /// </summary>
    Task<ProductCategoryListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 创建类目。
    /// </summary>
    /// <param name="input">创建类目的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的类目信息。</returns>
    Task<ProductCategoryListOutput> CreateAsync(CreateProductCategoryInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 更新指定类目。
    /// </summary>
    /// <param name="id">类目 ID。</param>
    /// <param name="input">更新类目的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(Guid id, UpdateProductCategoryInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 删除无下级类目的类目。
    /// </summary>
    /// <param name="id">类目 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取类目列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>类目只读列表。</returns>
    Task<IReadOnlyList<ProductCategoryListOutput>> GetListAsync(CancellationToken cancellationToken);
}
