namespace SeventyTwo.Sample.Domain.Inventories;

public interface IInventoryRepository
{
    Task ChangeAsync(InventoryChangeDraft draft, CancellationToken cancellationToken);
}
