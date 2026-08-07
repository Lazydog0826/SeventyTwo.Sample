using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable EmptyConstructor
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.Orders;

[SugarTable("orders")]
internal sealed class OrderRecord : BaseEntity
{
    public OrderRecord() { }

    /// <summary>
    /// 订单编号。
    /// </summary>
    [SugarColumn(ColumnName = "order_no", Length = 32, ColumnDescription = "订单编号")]
    public string OrderNo { get; init; } = string.Empty;

    /// <summary>
    /// 客户 ID。
    /// </summary>
    [SugarColumn(ColumnName = "customer_id", ColumnDescription = "客户id", ColumnDataType = "uuid")]
    public Guid CustomerId { get; init; }

    /// <summary>
    /// 仓库 ID。
    /// </summary>
    [SugarColumn(ColumnName = "warehouse_id", ColumnDescription = "仓库id", ColumnDataType = "uuid")]
    public Guid WarehouseId { get; init; }

    /// <summary>
    /// 订单类型：1 销售订单，2 退货订单，3 调拨订单。
    /// </summary>
    [SugarColumn(ColumnName = "order_type", ColumnDescription = "订单类型：1销售订单，2退货订单，3调拨订单")]
    public OrderType OrderType { get; init; }

    /// <summary>
    /// 订单状态：0 待处理，1 处理中，2 已处理，3 已取消。
    /// </summary>
    [SugarColumn(ColumnName = "order_status", ColumnDescription = "订单状态：0待处理，1处理中，2已处理，3已取消")]
    public OrderStatus OrderStatus { get; init; }

    /// <summary>
    /// 收货人姓名。
    /// </summary>
    [SugarColumn(ColumnName = "receiver_name", Length = 50, IsNullable = true, ColumnDescription = "收货人姓名")]
    public string? ReceiverName { get; init; }

    /// <summary>
    /// 收货人手机号。
    /// </summary>
    [SugarColumn(ColumnName = "receiver_phone", Length = 20, IsNullable = true, ColumnDescription = "收货人手机号")]
    public string? ReceiverPhone { get; init; }

    /// <summary>
    /// 收货地址所在省份。
    /// </summary>
    [SugarColumn(ColumnName = "province", Length = 30, IsNullable = true, ColumnDescription = "省份")]
    public string? Province { get; init; }

    /// <summary>
    /// 收货地址所在城市。
    /// </summary>
    [SugarColumn(ColumnName = "city", Length = 30, IsNullable = true, ColumnDescription = "城市")]
    public string? City { get; init; }

    /// <summary>
    /// 收货地址所在区县。
    /// </summary>
    [SugarColumn(ColumnName = "district", Length = 30, IsNullable = true, ColumnDescription = "区县")]
    public string? District { get; init; }

    /// <summary>
    /// 收货详细地址。
    /// </summary>
    [SugarColumn(ColumnName = "detail_address", Length = 200, IsNullable = true, ColumnDescription = "详细地址")]
    public string? DetailAddress { get; init; }

    /// <summary>
    /// 订单备注。
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "订单备注")]
    public string? Remark { get; init; }

    [SugarColumn(IsIgnore = true)]
    public List<OrderItemRecord> Items { get; set; } = [];
}

[SugarTable("order_items")]
internal sealed class OrderItemRecord
{
    public OrderItemRecord() { }

    /// <summary>
    /// 订单明细主键。
    /// </summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, ColumnDataType = "uuid", ColumnDescription = "订单明细主键")]
    public Guid Id { get; init; }

    /// <summary>
    /// 所属订单 ID。
    /// </summary>
    [SugarColumn(ColumnName = "order_id", ColumnDescription = "订单主表id", ColumnDataType = "uuid")]
    public Guid OrderId { get; init; }

    /// <summary>
    /// 订单内明细行号。
    /// </summary>
    [SugarColumn(ColumnName = "line_no", ColumnDescription = "订单内明细行号")]
    public int LineNo { get; init; }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    [SugarColumn(ColumnName = "product_id", ColumnDescription = "商品id", ColumnDataType = "uuid")]
    public Guid ProductId { get; init; }

    /// <summary>
    /// 下单时的商品名称快照。
    /// </summary>
    [SugarColumn(ColumnName = "product_name", Length = 255, ColumnDescription = "商品名称快照")]
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// 商品计量单位。
    /// </summary>
    [SugarColumn(ColumnName = "unit", Length = 20, IsNullable = true, ColumnDescription = "计量单位")]
    public string? Unit { get; init; }

    /// <summary>
    /// 购买数量。
    /// </summary>
    [SugarColumn(ColumnName = "quantity", ColumnDescription = "购买数量")]
    public int Quantity { get; init; }

    /// <summary>
    /// 下单时的商品单价。
    /// </summary>
    [SugarColumn(ColumnName = "unit_price", Length = 18, DecimalDigits = 2, ColumnDescription = "商品单价")]
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// 已发货数量。
    /// </summary>
    [SugarColumn(ColumnName = "shipped_quantity", ColumnDescription = "已发货数量")]
    public int ShippedQuantity { get; init; }

    /// <summary>
    /// 已退货数量。
    /// </summary>
    [SugarColumn(ColumnName = "returned_quantity", ColumnDescription = "已退货数量")]
    public int ReturnedQuantity { get; init; }

    /// <summary>
    /// 订单明细备注。
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 300, IsNullable = true, ColumnDescription = "明细备注")]
    public string? Remark { get; init; }
}
