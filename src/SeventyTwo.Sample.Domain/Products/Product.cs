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
    /// <param name="code">商品编码。</param>
    /// <param name="description">商品描述。</param>
    /// <param name="unit">计量单位。</param>
    /// <param name="categoryId">所属类目 ID；未归属类目时为 <see langword="null"/>。</param>
    /// <param name="status">上架状态，默认下架。</param>
    public Product(
        Guid id,
        string name,
        decimal price,
        string code,
        string? description = null,
        string? unit = null,
        Guid? categoryId = null,
        ProductStatus status = ProductStatus.OffShelf
    )
    {
        if (id == Guid.Empty)
        {
            throw new ProductDomainException(MessageKeys.Products.IdRequired);
        }

        Id = id;
        Enable = true;
        SetInfo(name, price, code, description, unit, categoryId, status);
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
    /// 商品编码，全局唯一。
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// 商品描述。
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 计量单位，如：件、箱、千克。
    /// </summary>
    public string? Unit { get; private set; }

    /// <summary>
    /// 所属商品类目 ID；未归属类目时为 <see langword="null"/>。
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// 上架状态。
    /// </summary>
    public ProductStatus Status { get; private set; } = ProductStatus.OffShelf;

    /// <summary>
    /// 修改商品基础信息。
    /// </summary>
    /// <param name="name">商品名称。</param>
    /// <param name="price">商品价格。</param>
    /// <param name="code">商品编码。</param>
    /// <param name="description">商品描述。</param>
    /// <param name="unit">计量单位。</param>
    /// <param name="categoryId">所属类目 ID；未归属类目时为 <see langword="null"/>。</param>
    /// <param name="status">上架状态。</param>
    /// <param name="version">客户端持有的商品版本 UUIDv7。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    public void Update(
        string name,
        decimal price,
        string code,
        string? description,
        string? unit,
        Guid? categoryId,
        ProductStatus status,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt
    )
    {
        if (version != Version)
        {
            throw new ProductDomainException(MessageKeys.Products.DataChanged, DomainErrorType.Conflict);
        }

        if (updatedAt == default)
        {
            throw new ProductDomainException(MessageKeys.Products.ModifiedAtRequired);
        }

        SetInfo(name, price, code, description, unit, categoryId, status);
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 切换上架状态。
    /// </summary>
    /// <param name="status">目标上架状态。</param>
    /// <param name="version">客户端持有的商品版本 UUIDv7。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    public void ChangeStatus(ProductStatus status, Guid version, Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (version != Version)
        {
            throw new ProductDomainException(MessageKeys.Products.DataChanged, DomainErrorType.Conflict);
        }

        if (updatedAt == default)
        {
            throw new ProductDomainException(MessageKeys.Products.ModifiedAtRequired);
        }

        if (!Enum.IsDefined(status))
        {
            throw new ProductDomainException(MessageKeys.Products.StatusInvalid);
        }

        Status = status;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 校验商品在当前版本下允许物理删除。
    /// </summary>
    /// <param name="version">客户端持有的商品版本 UUIDv7。</param>
    public void EnsureCanDelete(Guid version)
    {
        if (version != Version)
        {
            throw new ProductDomainException(MessageKeys.Products.DataChanged, DomainErrorType.Conflict);
        }
    }

    /// <summary>
    /// 校验并设置商品基础信息。
    /// </summary>
    /// <param name="name">商品名称。</param>
    /// <param name="price">商品价格。</param>
    /// <param name="code">商品编码。</param>
    /// <param name="description">商品描述。</param>
    /// <param name="unit">计量单位。</param>
    /// <param name="categoryId">所属类目 ID。</param>
    /// <param name="status">上架状态。</param>
    private void SetInfo(
        string name,
        decimal price,
        string code,
        string? description,
        string? unit,
        Guid? categoryId,
        ProductStatus status
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ProductDomainException(MessageKeys.Products.NameRequired);
        }

        name = name.Trim();
        if (name.Length > 255)
        {
            throw new ProductDomainException(MessageKeys.Products.NameTooLong);
        }

        if (price <= 0)
        {
            throw new ProductDomainException(MessageKeys.Products.PriceMustBePositive);
        }

        if (price > MaxPrice)
        {
            throw new ProductDomainException(MessageKeys.Products.PriceOutOfRange);
        }

        if (decimal.Round(price, 2) != price)
        {
            throw new ProductDomainException(MessageKeys.Products.PriceScaleInvalid);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ProductDomainException(MessageKeys.Products.CodeRequired);
        }

        code = code.Trim();
        if (code.Length > 64)
        {
            throw new ProductDomainException(MessageKeys.Products.CodeTooLong);
        }

        description = NormalizeOptional(description);
        if (description?.Length > 2000)
        {
            throw new ProductDomainException(MessageKeys.Products.DescriptionTooLong);
        }

        unit = NormalizeOptional(unit);
        if (unit?.Length > 20)
        {
            throw new ProductDomainException(MessageKeys.Products.UnitTooLong);
        }

        if (!Enum.IsDefined(status))
        {
            throw new ProductDomainException(MessageKeys.Products.StatusInvalid);
        }

        Name = name;
        Price = price;
        Code = code;
        Description = description;
        Unit = unit;
        CategoryId = categoryId;
        Status = status;
    }

    /// <summary>
    /// 可选文本统一去空白并归一为 null，避免落库空串。
    /// </summary>
    /// <param name="value">原始输入。</param>
    /// <returns>归一后的文本。</returns>
    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
