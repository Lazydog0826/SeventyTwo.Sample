using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Application.Inventories.ChangeInventory;

public interface IChangeInventoryService
{
    Task ChangeAsync(InventoryChangeDraft draft, CancellationToken cancellationToken);
}
