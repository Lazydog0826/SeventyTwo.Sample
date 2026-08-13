// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.WebApi.Contracts.Products;

/// <summary>
/// 商品创建请求。
/// </summary>
/// <param name="Name">商品名称。</param>
/// <param name="Price">商品价格。</param>
public sealed record CreateProductRequest(string Name, decimal Price);

/// <summary>
/// 商品修改请求。
/// </summary>
/// <param name="Id">商品标识。</param>
/// <param name="Name">商品名称。</param>
/// <param name="Price">商品价格。</param>
/// <param name="Version">并发版本。</param>
public sealed record UpdateProductRequest(Guid Id, string Name, decimal Price, Guid Version);

/// <summary>
/// 商品删除请求。
/// </summary>
/// <param name="Id">商品标识。</param>
public sealed record DeleteProductRequest(Guid Id);

/// <summary>
/// 商品详情查询请求。
/// </summary>
/// <param name="Id">商品标识。</param>
public sealed record GetProductRequest(Guid Id);
