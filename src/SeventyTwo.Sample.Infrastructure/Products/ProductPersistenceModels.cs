using SeventyTwo.InfraKit.Core.DomainAggregateRoot;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Products;

[SugarTable("product_record")]
internal sealed class ProductRecord : BaseEntity
{
    /// <summary>
    /// 商品名称。
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品价格。
    /// </summary>
    [SugarColumn(ColumnName = "price")]
    public decimal Price { get; set; }
}
