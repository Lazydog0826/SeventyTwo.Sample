namespace SeventyTwo.Sample.WebApi.Contracts.Products;

/// <summary>
/// 商品类目创建请求。
/// </summary>
/// <param name="Name">类目名称。</param>
/// <param name="ParentId">上级类目 ID；为空时创建顶级类目。</param>
public record CreateProductCategoryRequest(string Name, Guid? ParentId = null);

/// <summary>
/// 商品类目修改请求。
/// </summary>
/// <param name="Id">类目 ID。</param>
/// <param name="Name">类目名称。</param>
/// <param name="ParentId">上级类目 ID；顶级类目为 null。</param>
/// <param name="Version">客户端持有的并发版本。</param>
public sealed record UpdateProductCategoryRequest(Guid Id, string Name, Guid? ParentId, Guid Version)
    : CreateProductCategoryRequest(Name, ParentId);

/// <summary>
/// 商品类目删除请求。
/// </summary>
/// <param name="Id">类目 ID。</param>
public sealed record DeleteProductCategoryRequest(Guid Id);
