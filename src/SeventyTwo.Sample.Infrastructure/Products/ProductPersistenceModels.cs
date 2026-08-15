using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Products;

[SugarTable("product_record")]
[SugarIndex("uq_product_record_code", nameof(Code), OrderByType.Asc, true)]
internal sealed class ProductRecord : BaseEntity, IAudited, IOrgScoped
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

    /// <summary>
    /// 商品编码，全局唯一。
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 64)]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 商品描述。
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 2000, IsNullable = true)]
    public string? Description { get; init; }

    /// <summary>
    /// 计量单位。
    /// </summary>
    [SugarColumn(ColumnName = "unit", Length = 20, IsNullable = true)]
    public string? Unit { get; init; }

    /// <summary>
    /// 所属商品类目 ID；未归属类目为 null。
    /// </summary>
    [SugarColumn(ColumnName = "category_id", IsNullable = true, ColumnDataType = "uuid")]
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// 上架状态：0下架，1上架。
    /// </summary>
    [SugarColumn(ColumnName = "status", ColumnDescription = "商品状态：0下架，1上架")]
    public ProductStatus Status { get; init; }
}

[SugarTable("product_category_record")]
[SugarIndex("ix_product_category_record_parent_id", nameof(ParentId), OrderByType.Asc)]
internal sealed class ProductCategoryRecord : BaseEntity, IAudited, IOrgScoped
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
