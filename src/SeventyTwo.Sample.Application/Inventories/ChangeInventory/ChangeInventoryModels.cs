namespace SeventyTwo.Sample.Application.Inventories.ChangeInventory;

public sealed record ChangeInventoryInput(
    string RequestNo,
    IReadOnlyCollection<InventoryIncreaseInput> Increases,
    IReadOnlyCollection<InventoryDecreaseInput> Decreases
);

public sealed record InventoryIncreaseInput(
    string ProductId,
    string WarehouseId,
    string LocationId,
    int Quantity,
    string InboundBatchNo,
    DateTimeOffset ChangedAt
);

public sealed record InventoryDecreaseInput(string ProductId, string WarehouseId, string LocationId, int Quantity);
