using SeventyTwo.Sample.Domain;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Inventories;

public sealed class Inventory : AggregateRoot
{
    public Inventory(
        string id,
        string productId,
        string warehouseId,
        string locationId,
        string inboundBatchNo,
        DateTimeOffset inboundAt,
        int quantity
    )
        : this(id, productId, warehouseId, locationId, inboundBatchNo, inboundAt, quantity, quantity) { }

    public Inventory(
        string id,
        string productId,
        string warehouseId,
        string locationId,
        string inboundBatchNo,
        DateTimeOffset inboundAt,
        int initialQuantity,
        int quantity
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InventoryDomainException("库存 ID 不能为空");
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new InventoryDomainException("商品 ID 不能为空");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            throw new InventoryDomainException("仓库 ID 不能为空");
        }

        if (string.IsNullOrWhiteSpace(locationId))
        {
            throw new InventoryDomainException("货位 ID 不能为空");
        }

        if (string.IsNullOrWhiteSpace(inboundBatchNo))
        {
            throw new InventoryDomainException("入库批次号不能为空");
        }

        if (inboundAt == default)
        {
            throw new InventoryDomainException("入库时间不能为空");
        }

        if (initialQuantity < 0)
        {
            throw new InventoryDomainException("初始库存数量不能小于 0");
        }

        if (quantity < 0)
        {
            throw new InventoryDomainException("库存数量不能小于 0");
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
    public string ProductId { get; private set; }

    /// <summary>
    /// 仓库 ID。
    /// </summary>
    public string WarehouseId { get; private set; }

    /// <summary>
    /// 货位 ID。
    /// </summary>
    public string LocationId { get; private set; }

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
            throw new InventoryDomainException("库存变更数量必须大于 0");
        }

        if (quantity > int.MaxValue - Quantity)
        {
            throw new InventoryDomainException("库存数量超出范围");
        }

        var beforeQuantity = Quantity;
        Quantity += quantity;
        return new InventoryQuantityChange(beforeQuantity, Quantity);
    }

    public InventoryQuantityChange Decrease(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException("库存变更数量必须大于 0");
        }

        if (quantity > Quantity)
        {
            throw new InventoryDomainException("库存不足");
        }

        var beforeQuantity = Quantity;
        Quantity -= quantity;
        return new InventoryQuantityChange(beforeQuantity, Quantity);
    }
}

public sealed record InventoryQuantityChange(int BeforeQuantity, int AfterQuantity);
