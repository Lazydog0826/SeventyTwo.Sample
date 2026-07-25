using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Application.Inventories.ChangeInventory;

public sealed record ChangeInventoryInput(
    string RequestNo,
    long InventoryId,
    InventoryChangeType ChangeType,
    int Quantity
);

public sealed record ChangeInventoryResult(
    long ChangeId,
    string RequestNo,
    long InventoryId,
    InventoryChangeType ChangeType,
    int Quantity,
    int BeforeQuantity,
    int AfterQuantity
);
