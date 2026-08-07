using SeventyTwo.Sample.Domain.Inventories;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.Inventories;

[SugarTable("inventory_record")]
internal sealed class InventoryRecord : BaseEntity
{
    /// <summary>
    /// 库存维度键
    /// </summary>
    [SugarColumn(ColumnName = "key")]
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// 商品 ID。
    /// </summary>
    [SugarColumn(ColumnName = "product_id", ColumnDataType = "uuid")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// 仓库 ID。
    /// </summary>
    [SugarColumn(ColumnName = "warehouse_id", ColumnDataType = "uuid")]
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// 库位 ID。
    /// </summary>
    [SugarColumn(ColumnName = "location_id", ColumnDataType = "uuid")]
    public Guid LocationId { get; set; }

    /// <summary>
    /// 入库批次号。
    /// </summary>
    [SugarColumn(ColumnName = "inbound_batch_no")]
    public string InboundBatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 入库时间。
    /// </summary>
    [SugarColumn(ColumnName = "inbound_at")]
    public DateTimeOffset InboundAt { get; init; }

    /// <summary>
    /// 初始库存数量。
    /// </summary>
    [SugarColumn(ColumnName = "initial_quantity")]
    public int InitialQuantity { get; set; }

    /// <summary>
    /// 当前库存数量。
    /// </summary>
    [SugarColumn(ColumnName = "quantity")]
    public int Quantity { get; set; }
}

[SugarTable("inventory_change_record")]
internal sealed class InventoryChangeRecord
{
    /// <summary>
    /// 库存变更记录 ID。
    /// </summary>
    [SugarColumn(ColumnName = "change_id", IsPrimaryKey = true, ColumnDataType = "uuid")]
    public Guid ChangeId { get; set; }

    /// <summary>
    /// 业务请求号 UUIDv7。
    /// </summary>
    [SugarColumn(ColumnName = "request_no", ColumnDataType = "uuid")]
    public Guid RequestNo { get; set; }

    /// <summary>
    /// 库存 ID。
    /// </summary>
    [SugarColumn(ColumnName = "inventory_id", ColumnDataType = "uuid")]
    public Guid InventoryId { get; set; }

    /// <summary>
    /// 变更类型：1 增加，2 减少。
    /// </summary>
    [SugarColumn(ColumnName = "change_type")]
    public InventoryChangeType ChangeType { get; set; }

    /// <summary>
    /// 变更数量。
    /// </summary>
    [SugarColumn(ColumnName = "quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// 变更前库存数量。
    /// </summary>
    [SugarColumn(ColumnName = "before_quantity")]
    public int BeforeQuantity { get; set; }

    /// <summary>
    /// 变更后库存数量。
    /// </summary>
    [SugarColumn(ColumnName = "after_quantity")]
    public int AfterQuantity { get; set; }

    /// <summary>
    /// 变更时间。
    /// </summary>
    [SugarColumn(ColumnName = "changed_at")]
    public DateTimeOffset ChangedAt { get; set; }
}

[SugarTable("inventory_change_request")]
internal sealed class InventoryChangeRequest
{
    /// <summary>
    /// 业务请求号 UUIDv7（唯一约束）
    /// </summary>
    [SugarColumn(ColumnName = "request_no", IsPrimaryKey = true, ColumnDataType = "uuid")]
    public Guid RequestNo { get; set; }

    /// <summary>
    /// 请求时间。
    /// </summary>
    [SugarColumn(ColumnName = "request_at")]
    public DateTimeOffset RequestAt { get; set; }
}

[SugarTable("inventory_change_lock")]
internal sealed class InventoryChangeLock
{
    /// <summary>
    /// 锁KEY（唯一约束）
    /// </summary>
    [SugarColumn(ColumnName = "lock_key", IsPrimaryKey = true, Length = 255)]
    public string LockKey { get; init; } = string.Empty;
}
