namespace SeventyTwo.Sample.Domain.Products;

/// <summary>
/// 商品上架状态。
/// </summary>
public enum ProductStatus : short
{
    /// <summary>
    /// 下架。
    /// </summary>
    OffShelf = 0,

    /// <summary>
    /// 上架。
    /// </summary>
    OnShelf = 1,
}
