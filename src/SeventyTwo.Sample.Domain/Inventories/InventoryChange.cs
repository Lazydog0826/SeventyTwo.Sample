namespace SeventyTwo.Sample.Domain.Inventories;

public enum InventoryChangeType : short
{
    Increase = 1,
    Decrease = 2,
}

public record InventoryDraft
{
    protected InventoryDraft(long productId, long warehouseId, long locationId, int quantity)
    {
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

        if (quantity <= 0)
        {
            throw new InventoryDomainException("库存变更数量必须大于 0");
        }

        ProductId = productId;
        WarehouseId = warehouseId;
        LocationId = locationId;
        Quantity = quantity;
    }

    public long ProductId { get; }

    public long WarehouseId { get; }

    public long LocationId { get; }

    public int Quantity { get; }
}

public sealed record InventoryIncreaseDraft : InventoryDraft
{
    public InventoryIncreaseDraft(
        long productId,
        long warehouseId,
        long locationId,
        int quantity,
        string inboundBatchNo,
        DateTimeOffset changedAt
    )
        : base(productId, warehouseId, locationId, quantity)
    {
        if (string.IsNullOrWhiteSpace(inboundBatchNo))
        {
            throw new InventoryDomainException("入库批次号不能为空");
        }

        if (inboundBatchNo.Length > 64)
        {
            throw new InventoryDomainException("入库批次号长度不能超过 64 个字符");
        }

        if (changedAt == default)
        {
            throw new InventoryDomainException("入库时间不能为空");
        }

        InboundBatchNo = inboundBatchNo;
        ChangedAt = changedAt;
    }

    public string InboundBatchNo { get; }

    public DateTimeOffset ChangedAt { get; }
}

public sealed record InventoryDecreaseDraft : InventoryDraft
{
    public InventoryDecreaseDraft(long productId, long warehouseId, long locationId, int quantity)
        : base(productId, warehouseId, locationId, quantity) { }
}
