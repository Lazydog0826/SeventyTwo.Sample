using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Orders;

[SugarTable("orders")]
internal sealed class OrderRecord
{
    public OrderRecord() { }

    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, ColumnDescription = "订单主键")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "order_no", Length = 32, ColumnDescription = "订单编号")]
    public string OrderNo { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "customer_id", ColumnDescription = "客户id")]
    public long CustomerId { get; set; }

    [SugarColumn(ColumnName = "warehouse_id", ColumnDescription = "仓库id")]
    public int WarehouseId { get; set; }

    [SugarColumn(ColumnName = "order_type", ColumnDescription = "订单类型：1销售订单，2退货订单，3调拨订单")]
    public short OrderType { get; set; }

    [SugarColumn(ColumnName = "order_status", ColumnDescription = "订单状态：0待审核，1已审核，2处理中，3已完成，4已取消")]
    public short OrderStatus { get; set; }

    [SugarColumn(ColumnName = "payment_status", ColumnDescription = "支付状态：0未支付，1部分支付，2已支付")]
    public short PaymentStatus { get; set; }

    [SugarColumn(ColumnName = "shipping_status", ColumnDescription = "发货状态：0未发货，1部分发货，2已发货")]
    public short ShippingStatus { get; set; }

    [SugarColumn(ColumnName = "total_amount", Length = 14, DecimalDigits = 2, ColumnDescription = "商品总金额")]
    public decimal TotalAmount { get; set; }

    [SugarColumn(ColumnName = "discount_amount", Length = 14, DecimalDigits = 2, ColumnDescription = "优惠金额")]
    public decimal DiscountAmount { get; set; }

    [SugarColumn(ColumnName = "freight_amount", Length = 14, DecimalDigits = 2, ColumnDescription = "运费金额")]
    public decimal FreightAmount { get; set; }

    [SugarColumn(ColumnName = "payable_amount", Length = 14, DecimalDigits = 2, ColumnDescription = "应付金额")]
    public decimal PayableAmount { get; set; }

    [SugarColumn(ColumnName = "item_count", ColumnDescription = "商品总数量")]
    public int ItemCount { get; set; }

    [SugarColumn(ColumnName = "receiver_name", Length = 50, IsNullable = true, ColumnDescription = "收货人姓名")]
    public string? ReceiverName { get; set; }

    [SugarColumn(ColumnName = "receiver_phone", Length = 20, IsNullable = true, ColumnDescription = "收货人手机号")]
    public string? ReceiverPhone { get; set; }

    [SugarColumn(ColumnName = "province", Length = 30, IsNullable = true, ColumnDescription = "省份")]
    public string? Province { get; set; }

    [SugarColumn(ColumnName = "city", Length = 30, IsNullable = true, ColumnDescription = "城市")]
    public string? City { get; set; }

    [SugarColumn(ColumnName = "district", Length = 30, IsNullable = true, ColumnDescription = "区县")]
    public string? District { get; set; }

    [SugarColumn(ColumnName = "detail_address", Length = 200, IsNullable = true, ColumnDescription = "详细地址")]
    public string? DetailAddress { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "订单备注")]
    public string? Remark { get; set; }

    [SugarColumn(ColumnName = "version", IsEnableUpdateVersionValidation = true, ColumnDescription = "乐观锁版本号")]
    public int Version { get; set; }

    [SugarColumn(ColumnName = "created_by", IsNullable = true, ColumnDescription = "创建人id")]
    public long? CreatedBy { get; set; }

    [SugarColumn(ColumnName = "created_at", ColumnDescription = "创建时间")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_by", IsNullable = true, ColumnDescription = "最后修改人id")]
    public long? UpdatedBy { get; set; }

    [SugarColumn(ColumnName = "updated_at", ColumnDescription = "最后修改时间")]
    public DateTime UpdatedAt { get; set; }
}

[SugarTable("order_items")]
internal sealed class OrderItemRecord
{
    public OrderItemRecord() { }

    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "订单明细主键")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "order_id", ColumnDescription = "订单主表id")]
    public long OrderId { get; set; }

    [SugarColumn(ColumnName = "line_no", ColumnDescription = "订单内明细行号")]
    public int LineNo { get; set; }

    [SugarColumn(ColumnName = "product_id", ColumnDescription = "商品id")]
    public long ProductId { get; set; }

    [SugarColumn(ColumnName = "sku_id", ColumnDescription = "商品sku id")]
    public long SkuId { get; set; }

    [SugarColumn(ColumnName = "sku_code", Length = 50, ColumnDescription = "sku编码")]
    public string SkuCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "product_name", Length = 100, ColumnDescription = "商品名称快照")]
    public string ProductName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "specification", Length = 100, IsNullable = true, ColumnDescription = "商品规格快照")]
    public string? Specification { get; set; }

    [SugarColumn(ColumnName = "unit", Length = 20, IsNullable = true, ColumnDescription = "计量单位")]
    public string? Unit { get; set; }

    [SugarColumn(ColumnName = "quantity", ColumnDescription = "购买数量")]
    public int Quantity { get; set; }

    [SugarColumn(ColumnName = "unit_price", Length = 14, DecimalDigits = 2, ColumnDescription = "商品单价")]
    public decimal UnitPrice { get; set; }

    [SugarColumn(ColumnName = "discount_amount", Length = 14, DecimalDigits = 2, ColumnDescription = "明细优惠金额")]
    public decimal DiscountAmount { get; set; }

    [SugarColumn(ColumnName = "line_amount", Length = 14, DecimalDigits = 2, ColumnDescription = "明细金额")]
    public decimal LineAmount { get; set; }

    [SugarColumn(ColumnName = "shipped_quantity", ColumnDescription = "已发货数量")]
    public int ShippedQuantity { get; set; }

    [SugarColumn(ColumnName = "returned_quantity", ColumnDescription = "已退货数量")]
    public int ReturnedQuantity { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 300, IsNullable = true, ColumnDescription = "明细备注")]
    public string? Remark { get; set; }

    [SugarColumn(ColumnName = "created_at", ColumnDescription = "创建时间")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at", ColumnDescription = "最后修改时间")]
    public DateTime UpdatedAt { get; set; }
}
