using SeventyTwo.InfraKit.Core.DomainAggregateRoot;
using SqlSugar;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Inventories;

public sealed class Inventory : AggregateRoot
{
    private Inventory() { }

    public Inventory(
        long id,
        long productId,
        long warehouseId,
        long locationId,
        string inboundBatchNo,
        DateTimeOffset inboundAt,
        int quantity
    )
    {
        if (id <= 0)
        {
            throw new InventoryDomainException("库存 ID 必须大于 0");
        }

        if (productId <= 0)
        {
            throw new InventoryDomainException("商品 ID 必须大于 0");
        }

        if (warehouseId <= 0)
        {
            throw new InventoryDomainException("仓库 ID 必须大于 0");
        }

        if (locationId <= 0)
        {
            throw new InventoryDomainException("货位 ID 必须大于 0");
        }

        if (string.IsNullOrWhiteSpace(inboundBatchNo))
        {
            throw new InventoryDomainException("入库批次号不能为空");
        }

        if (inboundAt == default)
        {
            throw new InventoryDomainException("入库时间不能为空");
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
        InitialQuantity = quantity;
        Quantity = quantity;
    }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    [SugarColumn(ColumnDescription = "商品 ID")]
    public long ProductId { get; private set; }

    /// <summary>
    /// 仓库 ID。
    /// </summary>
    [SugarColumn(ColumnDescription = "仓库 ID")]
    public long WarehouseId { get; private set; }

    /// <summary>
    /// 货位 ID。
    /// </summary>
    [SugarColumn(ColumnDescription = "货位 ID")]
    public long LocationId { get; private set; }

    /// <summary>
    /// 入库批次号。
    /// </summary>
    [SugarColumn(ColumnDescription = "入库批次号")]
    public string InboundBatchNo { get; private set; } = string.Empty;

    /// <summary>
    /// 入库时间。
    /// </summary>
    [SugarColumn(ColumnDescription = "入库时间")]
    public DateTimeOffset InboundAt { get; private set; }

    /// <summary>
    /// 初始入库数量。
    /// </summary>
    [SugarColumn(ColumnDescription = "初始入库数量")]
    public int InitialQuantity { get; private set; }

    /// <summary>
    /// 当前库存数量。
    /// </summary>
    [SugarColumn(ColumnDescription = "当前库存数量")]
    public int Quantity { get; private set; }
}
