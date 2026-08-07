namespace SeventyTwo.Sample.Application.Inventories.ChangeInventory;

public sealed record ChangeInventoryInput(
    Guid RequestNo,
    IReadOnlyCollection<InventoryIncreaseInput> Increases,
    IReadOnlyCollection<InventoryDecreaseInput> Decreases
);

public sealed record InventoryIncreaseInput(
    Guid ProductId,
    Guid WarehouseId,
    Guid LocationId,
    int Quantity,
    string InboundBatchNo,
    DateTimeOffset ChangedAt
);

public sealed record InventoryDecreaseInput(Guid ProductId, Guid WarehouseId, Guid LocationId, int Quantity);
