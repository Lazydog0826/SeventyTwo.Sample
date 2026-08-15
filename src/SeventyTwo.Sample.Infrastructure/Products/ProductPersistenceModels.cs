using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Products;

[SugarTable("product_record")]
internal sealed class ProductRecord : BaseEntity
{
    /// <summary>
    /// 商品名称。
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 商品价格。
    /// </summary>
    [SugarColumn(ColumnName = "price")]
    public decimal Price { get; init; }
}

[SugarTable("product_category_record")]
[SugarIndex("ix_product_category_record_parent_id", nameof(ParentId), OrderByType.Asc)]
internal sealed class ProductCategoryRecord : BaseEntity
{
    /// <summary>
    /// 类目名称。
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 上级类目 ID；顶级类目为 null。
    /// </summary>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true, ColumnDataType = "uuid")]
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 排序号，同级内按升序展示。
    /// </summary>
    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; init; }

    /// <summary>
    /// 由类目 ID 组成的完整层级路径。
    /// </summary>
    [SugarColumn(ColumnName = "path", ColumnDataType = "text")]
    public string Path { get; init; } = string.Empty;
}
