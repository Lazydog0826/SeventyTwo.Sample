namespace SeventyTwo.Sample.Domain.Inventories;

public interface IInventoryRepository
{
    Task ChangeAsync(
        List<InventoryIncreaseDraft> increases,
        List<InventoryDecreaseDraft> drafts,
        string requestNo,
        CancellationToken cancellationToken
    );
}
