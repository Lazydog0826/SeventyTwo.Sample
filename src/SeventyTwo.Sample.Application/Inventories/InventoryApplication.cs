using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Application.Inventories;

[AutofacDependency(typeof(IInventoryApplication))]
public sealed class InventoryApplication(IInventoryRepository inventoryRepository) : IInventoryApplication
{
    public async Task ChangeAsync(ChangeInventoryInput input, CancellationToken cancellationToken)
    {
        var increases = input
            .Increases.Select(x => new InventoryIncreaseDraft(
                x.ProductId,
                x.WarehouseId,
                x.LocationId,
                x.Quantity,
                x.InboundBatchNo,
                x.ChangedAt
            ))
            .ToList();
        var decreases = input
            .Decreases.Select(x => new InventoryDecreaseDraft(x.ProductId, x.WarehouseId, x.LocationId, x.Quantity))
            .ToList();

        var draft = new InventoryChangeDraft(input.RequestNo, increases, decreases);
        await inventoryRepository.ChangeAsync(draft, cancellationToken);
    }
}
