namespace SeventyTwo.Sample.Domain.Inventories;

public enum InventoryChangeType : short
{
    Increase = 1,
    Decrease = 2,
}

public record InventoryDraft(long ProductId, long WarehouseId, long LocationId, int Quantity);

public sealed record InventoryIncreaseDraft(
    long ProductId,
    long WarehouseId,
    long LocationId,
    int Quantity,
    string InboundBatchNo,
    DateTimeOffset ChangedAt
) : InventoryDraft(ProductId, WarehouseId, LocationId, Quantity);

public sealed record InventoryDecreaseDraft(long ProductId, long WarehouseId, long LocationId, int Quantity)
    : InventoryDraft(ProductId, WarehouseId, LocationId, Quantity);
