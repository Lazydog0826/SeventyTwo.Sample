namespace SeventyTwo.Sample.Domain.Inventories;

public enum InventoryChangeType : short
{
    Increase = 1,
    Decrease = 2,
}

public sealed record InventoryChange(
    long Id,
    string RequestNo,
    long InventoryId,
    InventoryChangeType ChangeType,
    int Quantity,
    int BeforeQuantity,
    int AfterQuantity,
    DateTimeOffset ChangedAt
);

public sealed record InventoryChangeDraft(
    long Id,
    string RequestNo,
    long InventoryId,
    InventoryChangeType ChangeType,
    int Quantity,
    DateTimeOffset ChangedAt
);
