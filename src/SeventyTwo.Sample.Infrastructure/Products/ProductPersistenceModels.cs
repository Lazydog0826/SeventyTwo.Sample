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
