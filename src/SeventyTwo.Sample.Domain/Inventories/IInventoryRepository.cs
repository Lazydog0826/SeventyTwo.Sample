namespace SeventyTwo.Sample.Domain.Inventories;

public interface IInventoryRepository
{
    Task<bool> TryRegisterChangeAsync(string requestNo, CancellationToken cancellationToken);

    Task EnsureChangeLocksAsync(
        IReadOnlyCollection<InventoryDimension> dimensions,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Inventory>> GetForChangeAsync(
        IReadOnlyCollection<InventoryDimension> dimensions,
        CancellationToken cancellationToken
    );

    Task SaveChangeAsync(
        string requestNo,
        IReadOnlyCollection<Inventory> newInventories,
        IReadOnlyCollection<Inventory> changedInventories,
        IReadOnlyCollection<InventoryChange> changes,
        CancellationToken cancellationToken
    );
}
