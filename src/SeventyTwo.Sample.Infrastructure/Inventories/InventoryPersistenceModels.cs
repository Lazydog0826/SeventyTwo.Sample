using SeventyTwo.InfraKit.Core.DomainAggregateRoot;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Inventories;

[SugarTable("inventories")]
internal sealed class InventoryRecord : BaseEntity
{
    [SugarColumn(ColumnName = "product_id")]
    public long ProductId { get; set; }

    [SugarColumn(ColumnName = "warehouse_id")]
    public long WarehouseId { get; set; }

    [SugarColumn(ColumnName = "location_id")]
    public long LocationId { get; set; }

    [SugarColumn(ColumnName = "inbound_batch_no")]
    public string InboundBatchNo { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "inbound_at")]
    public DateTimeOffset InboundAt { get; set; }

    [SugarColumn(ColumnName = "initial_quantity")]
    public int InitialQuantity { get; set; }

    [SugarColumn(ColumnName = "quantity")]
    public int Quantity { get; set; }
}

[SugarTable("inventory_changes")]
internal sealed class InventoryChangeRecord
{
    [SugarColumn(ColumnName = "change_id", IsPrimaryKey = true)]
    public long ChangeId { get; set; }

    [SugarColumn(ColumnName = "request_no")]
    public string RequestNo { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "inventory_id")]
    public long InventoryId { get; set; }

    [SugarColumn(ColumnName = "change_type")]
    public short ChangeType { get; set; }

    [SugarColumn(ColumnName = "quantity")]
    public int Quantity { get; set; }

    [SugarColumn(ColumnName = "before_quantity")]
    public int BeforeQuantity { get; set; }

    [SugarColumn(ColumnName = "after_quantity")]
    public int AfterQuantity { get; set; }

    [SugarColumn(ColumnName = "changed_at")]
    public DateTimeOffset ChangedAt { get; set; }
}
