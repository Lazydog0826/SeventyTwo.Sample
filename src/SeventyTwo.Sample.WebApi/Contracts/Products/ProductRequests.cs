// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.WebApi.Contracts.Products;

/// <summary>
/// 商品创建请求。
/// </summary>
/// <param name="Name">商品名称。</param>
/// <param name="Price">商品价格。</param>
/// <param name="Code">商品编码。</param>
/// <param name="Description">商品描述。</param>
/// <param name="Unit">计量单位。</param>
/// <param name="CategoryId">所属类目 ID；未归属类目时为 null。</param>
/// <param name="Status">上架状态，默认下架。</param>
public sealed record CreateProductRequest(
    string Name,
    decimal Price,
    string Code,
    string? Description = null,
    string? Unit = null,
    Guid? CategoryId = null,
    ProductStatus Status = ProductStatus.OffShelf
);

/// <summary>
/// 商品修改请求。
/// </summary>
/// <param name="Id">商品标识。</param>
/// <param name="Name">商品名称。</param>
/// <param name="Price">商品价格。</param>
/// <param name="Code">商品编码。</param>
/// <param name="Status">上架状态。</param>
/// <param name="Version">并发版本。</param>
/// <param name="Description">商品描述。</param>
/// <param name="Unit">计量单位。</param>
/// <param name="CategoryId">所属类目 ID；未归属类目时为 null。</param>
public sealed record UpdateProductRequest(
    Guid Id,
    string Name,
    decimal Price,
    string Code,
    ProductStatus Status,
    Guid Version,
    string? Description = null,
    string? Unit = null,
    Guid? CategoryId = null
);

/// <summary>
/// 商品删除请求。
/// </summary>
/// <param name="Id">商品标识。</param>
/// <param name="Version">并发版本。</param>
public sealed record DeleteProductRequest(Guid Id, Guid Version);

/// <summary>
/// 商品上架状态切换请求。
/// </summary>
/// <param name="Id">商品标识。</param>
/// <param name="Status">目标上架状态。</param>
/// <param name="Version">并发版本。</param>
public sealed record ChangeProductStatusRequest(Guid Id, ProductStatus Status, Guid Version);

/// <summary>
/// 商品详情查询请求。
/// </summary>
/// <param name="Id">商品标识。</param>
public sealed record GetProductRequest(Guid Id);
