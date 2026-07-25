namespace SeventyTwo.Sample.Domain.Orders;

public sealed record OrderItemDraft(
    long ProductId,
    long SkuId,
    string SkuCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public sealed class OrderItem
{
    internal OrderItem(
        long orderId,
        int lineNo,
        long productId,
        long skuId,
        string skuCode,
        string productName,
        int quantity,
        decimal unitPrice,
        DateTime createdAt
    )
    {
        OrderId = orderId;
        LineNo = lineNo;
        ProductId = productId;
        SkuId = skuId;
        SkuCode = skuCode;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineAmount = quantity * unitPrice;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public long Id { get; private set; }

    public long OrderId { get; private set; }

    public int LineNo { get; private set; }

    public long ProductId { get; private set; }

    public long SkuId { get; private set; }

    public string SkuCode { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public string? Specification { get; private set; }

    public string? Unit { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal LineAmount { get; private set; }

    public int ShippedQuantity { get; private set; }

    public int ReturnedQuantity { get; private set; }

    public string? Remark { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}
