namespace SeventyTwo.Sample.Application.Inventories.ChangeInventory;

public sealed record ChangeInventoryInput(
    string RequestNo,
    IReadOnlyCollection<InventoryIncreaseInput> Increases,
    IReadOnlyCollection<InventoryDecreaseInput> Decreases
);

public sealed record InventoryIncreaseInput(
    long ProductId,
    long WarehouseId,
    long LocationId,
    int Quantity,
    string InboundBatchNo,
    DateTimeOffset ChangedAt
);

public sealed record InventoryDecreaseInput(long ProductId, long WarehouseId, long LocationId, int Quantity);
