using SeventyTwo.InfraKit.Core.DomainAggregateRoot;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Products;

public sealed class Product : AggregateRoot
{
    private Product() { }

    public Product(long id, string name, decimal price)
    {
        if (id <= 0)
        {
            throw new ProductDomainException("商品 ID 必须大于 0");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ProductDomainException("商品名称不能为空");
        }

        if (price <= 0)
        {
            throw new ProductDomainException("商品价格必须大于 0");
        }

        Id = id;
        Name = name;
        Price = price;
    }

    /// <summary>
    /// 商品名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 商品价格。
    /// </summary>
    public decimal Price { get; private set; }
}
