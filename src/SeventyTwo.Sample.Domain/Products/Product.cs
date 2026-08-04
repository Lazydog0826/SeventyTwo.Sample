// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Products;

public sealed class Product : AggregateRoot
{
    private const decimal MaxPrice = 9999999999999999.99m;

    /// <summary>
    /// 供持久化组件还原商品聚合使用。
    /// </summary>
    private Product() { }

    /// <summary>
    /// 创建商品聚合。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="name">商品名称。</param>
    /// <param name="price">商品价格。</param>
    public Product(Guid id, string name, decimal price)
    {
        if (id == Guid.Empty)
        {
            throw new ProductDomainException("商品 ID 不能为空");
        }

        Id = id;
        Enable = true;
        SetInfo(name, price);
    }

    /// <summary>
    /// 商品名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 商品价格。
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// 修改商品基础信息。
    /// </summary>
    /// <param name="name">商品名称。</param>
    /// <param name="price">商品价格。</param>
    /// <param name="version">客户端持有的商品版本 UUIDv7。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    public void Update(string name, decimal price, Guid version, string updatedBy, DateTimeOffset updatedAt)
    {
        if (version != Version)
        {
            throw new ProductDomainException("商品数据已变更，请刷新后重试");
        }

        if (updatedAt == default)
        {
            throw new ProductDomainException("商品修改时间不能为空");
        }

        SetInfo(name, price);
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 将商品标记为已删除。
    /// </summary>
    /// <param name="deletedBy">删除人 ID。</param>
    /// <param name="deletedAt">删除时间。</param>
    public void Delete(string deletedBy, DateTimeOffset deletedAt)
    {
        if (deletedAt == default)
        {
            throw new ProductDomainException("商品删除时间不能为空");
        }

        Enable = false;
        DeleteBy = deletedBy;
        DeleteAt = deletedAt;
    }

    /// <summary>
    /// 校验并设置商品基础信息。
    /// </summary>
    /// <param name="name">商品名称。</param>
    /// <param name="price">商品价格。</param>
    private void SetInfo(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ProductDomainException("商品名称不能为空");
        }

        name = name.Trim();
        if (name.Length > 255)
        {
            throw new ProductDomainException("商品名称长度不能超过 255 个字符");
        }

        if (price <= 0)
        {
            throw new ProductDomainException("商品价格必须大于 0");
        }

        if (price > MaxPrice)
        {
            throw new ProductDomainException("商品价格超出范围");
        }

        if (decimal.Round(price, 2) != price)
        {
            throw new ProductDomainException("商品价格最多保留两位小数");
        }

        Name = name;
        Price = price;
    }
}
