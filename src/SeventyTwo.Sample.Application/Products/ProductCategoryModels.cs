// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.Application.Products;

/// <summary>
/// 商品类目列表项。
/// </summary>
public sealed record ProductCategoryListOutput
{
    /// <summary>
    /// 类目 ID。
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 类目名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 上级类目 ID；顶级类目为 <see langword="null"/>。
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 由类目 ID 组成的完整层级路径。
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 排序号，同级内按升序展示。
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// 并发版本。
    /// </summary>
    public Guid Version { get; init; }
}

/// <summary>
/// 创建商品类目的输入。
/// </summary>
/// <param name="Name">类目名称。</param>
/// <param name="ParentId">上级类目 ID；顶级类目为 <see langword="null"/>。</param>
/// <param name="SortOrder">排序号，同级内按升序展示。</param>
public record CreateProductCategoryInput(string Name, Guid? ParentId = null, int SortOrder = 0);

/// <summary>
/// 更新商品类目的输入。
/// </summary>
/// <param name="Name">类目名称。</param>
/// <param name="ParentId">上级类目 ID；顶级类目为 <see langword="null"/>。</param>
/// <param name="Version">客户端读取类目时获得的并发版本。</param>
/// <param name="SortOrder">排序号，同级内按升序展示。</param>
public sealed record UpdateProductCategoryInput(string Name, Guid? ParentId, Guid Version, int SortOrder = 0)
    : CreateProductCategoryInput(Name, ParentId, SortOrder);
