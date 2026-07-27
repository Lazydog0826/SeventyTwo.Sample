using SeventyTwo.Sample.Application.Inventories.ChangeInventory;

namespace SeventyTwo.Sample.Application.Inventories;

public interface IInventoryApplication
{
    /// <summary>
    /// 库存变更
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ChangeAsync(ChangeInventoryInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 仓储费计算（示例）
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyDictionary<string, decimal>> StorageFeeCalcAsync();
}
