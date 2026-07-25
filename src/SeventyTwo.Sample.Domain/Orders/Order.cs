namespace SeventyTwo.Sample.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public Order(
        long id,
        string orderNo,
        long customerId,
        int warehouseId,
        IReadOnlyCollection<OrderItemDraft> items,
        DateTime createdAt
    )
    {
        if (id <= 0)
        {
            throw new OrderDomainException("订单 ID 必须大于 0");
        }

        if (string.IsNullOrWhiteSpace(orderNo))
        {
            throw new OrderDomainException("订单号不能为空");
        }

        if (customerId <= 0)
        {
            throw new OrderDomainException("客户 ID 必须大于 0");
        }

        if (warehouseId <= 0)
        {
            throw new OrderDomainException("仓库 ID 必须大于 0");
        }

        if (items.Count == 0)
        {
            throw new OrderDomainException("订单至少包含一条明细");
        }

        Id = id;
        OrderNo = orderNo;
        CustomerId = customerId;
        WarehouseId = warehouseId;
        OrderType = OrderType.Sales;
        OrderStatus = OrderStatus.PendingReview;
        PaymentStatus = PaymentStatus.Unpaid;
        ShippingStatus = ShippingStatus.Unshipped;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        foreach (var item in items)
        {
            AddItem(item, createdAt);
        }
    }

    public long Id { get; private set; }

    public string OrderNo { get; private set; } = string.Empty;

    public long CustomerId { get; private set; }

    public int WarehouseId { get; private set; }

    public OrderType OrderType { get; private set; }

    public OrderStatus OrderStatus { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; }

    public ShippingStatus ShippingStatus { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal FreightAmount { get; private set; }

    public decimal PayableAmount { get; private set; }

    public int ItemCount { get; private set; }

    public string? ReceiverName { get; private set; }

    public string? ReceiverPhone { get; private set; }

    public string? Province { get; private set; }

    public string? City { get; private set; }

    public string? District { get; private set; }

    public string? DetailAddress { get; private set; }

    public string? Remark { get; private set; }

    public int Version { get; private set; }

    public long? CreatedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public long? UpdatedBy { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private void AddItem(OrderItemDraft item, DateTime createdAt)
    {
        if (item.ProductId <= 0)
        {
            throw new OrderDomainException("商品 ID 必须大于 0");
        }

        if (item.SkuId <= 0)
        {
            throw new OrderDomainException("SKU ID 必须大于 0");
        }

        if (string.IsNullOrWhiteSpace(item.SkuCode))
        {
            throw new OrderDomainException("SKU 编码不能为空");
        }

        if (string.IsNullOrWhiteSpace(item.ProductName))
        {
            throw new OrderDomainException("商品名称不能为空");
        }

        if (item.Quantity <= 0)
        {
            throw new OrderDomainException("购买数量必须大于 0");
        }

        if (item.UnitPrice <= 0)
        {
            throw new OrderDomainException("商品单价必须大于 0");
        }

        var orderItem = new OrderItem(
            Id,
            _items.Count + 1,
            item.ProductId,
            item.SkuId,
            item.SkuCode,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            createdAt
        );
        _items.Add(orderItem);
        ItemCount += orderItem.Quantity;
        TotalAmount += orderItem.LineAmount;
        PayableAmount = TotalAmount - DiscountAmount + FreightAmount;
        UpdatedAt = createdAt;
    }
}
