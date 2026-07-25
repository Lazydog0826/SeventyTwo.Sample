namespace SeventyTwo.Sample.Domain.Inventories;

public interface IInventoryRepository
{
    Task<InventoryChange> ChangeAsync(
        InventoryChangeDraft change,
        CancellationToken cancellationToken
    );
}
