// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Inventories;

public sealed class Inventory : AggregateRoot
{
    public Inventory(
        Guid id,
        Guid productId,
        Guid warehouseId,
        Guid locationId,
        string inboundBatchNo,
        DateTimeOffset inboundAt,
        int quantity
    )
        : this(id, productId, warehouseId, locationId, inboundBatchNo, inboundAt, quantity, quantity) { }

    public Inventory(
        Guid id,
        Guid productId,
        Guid warehouseId,
        Guid locationId,
        string inboundBatchNo,
        DateTimeOffset inboundAt,
        int initialQuantity,
        int quantity
    )
    {
        if (id == Guid.Empty)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.IdRequired);
        }

        if (productId == Guid.Empty)
        {
            throw new InventoryDomainException(MessageKeys.Products.IdRequired);
        }

        if (warehouseId == Guid.Empty)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.WarehouseIdRequired);
        }

        if (locationId == Guid.Empty)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.LocationIdRequired);
        }

        if (string.IsNullOrWhiteSpace(inboundBatchNo))
        {
            throw new InventoryDomainException(MessageKeys.Inventories.InboundBatchNoRequired);
        }

        if (inboundAt == default)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.InboundAtRequired);
        }

        if (initialQuantity < 0)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.InitialQuantityMustNotBeNegative);
        }

        if (quantity < 0)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.QuantityMustNotBeNegative);
        }

        Id = id;
        ProductId = productId;
        WarehouseId = warehouseId;
        LocationId = locationId;
        InboundBatchNo = inboundBatchNo;
        InboundAt = inboundAt;
        InitialQuantity = initialQuantity;
        Quantity = quantity;
    }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// 仓库 ID。
    /// </summary>
    public Guid WarehouseId { get; private set; }

    /// <summary>
    /// 货位 ID。
    /// </summary>
    public Guid LocationId { get; private set; }

    /// <summary>
    /// 入库批次号。
    /// </summary>
    public string InboundBatchNo { get; private set; }

    /// <summary>
    /// 入库时间。
    /// </summary>
    public DateTimeOffset InboundAt { get; private set; }

    /// <summary>
    /// 初始入库数量。
    /// </summary>
    public int InitialQuantity { get; private set; }

    /// <summary>
    /// 当前库存数量。
    /// </summary>
    public int Quantity { get; private set; }

    public InventoryQuantityChange Increase(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.ChangeQuantityMustBePositive);
        }

        if (quantity > int.MaxValue - Quantity)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.QuantityOutOfRange);
        }

        var beforeQuantity = Quantity;
        Quantity += quantity;
        return new InventoryQuantityChange(beforeQuantity, Quantity);
    }

    public InventoryQuantityChange Decrease(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.ChangeQuantityMustBePositive);
        }

        if (quantity > Quantity)
        {
            throw new InventoryDomainException(MessageKeys.Inventories.Insufficient, DomainErrorType.Conflict);
        }

        var beforeQuantity = Quantity;
        Quantity -= quantity;
        return new InventoryQuantityChange(beforeQuantity, Quantity);
    }
}

public sealed record InventoryQuantityChange(int BeforeQuantity, int AfterQuantity);
