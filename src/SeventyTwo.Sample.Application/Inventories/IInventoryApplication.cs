using SeventyTwo.Sample.Application.Inventories.ChangeInventory;

namespace SeventyTwo.Sample.Application.Inventories;

public interface IInventoryApplication
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ChangeAsync(ChangeInventoryInput input, CancellationToken cancellationToken);
}
