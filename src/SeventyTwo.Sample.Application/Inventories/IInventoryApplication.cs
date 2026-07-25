using SeventyTwo.Sample.Application.Inventories.ChangeInventory;

namespace SeventyTwo.Sample.Application.Inventories;

public interface IInventoryApplication
{
    Task<ChangeInventoryResult> ChangeAsync(
        ChangeInventoryInput input,
        CancellationToken cancellationToken
    );
}
