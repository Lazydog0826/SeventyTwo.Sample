using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Application.Inventories;

[AutofacDependency(typeof(IInventoryApplication))]
public sealed class InventoryApplication(IInventoryRepository inventoryRepository) : IInventoryApplication
{
    public async Task ChangeAsync(ChangeInventoryInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.RequestNo))
        {
            throw new InventoryDomainException("业务请求号不能为空");
        }

        if (input.RequestNo.Length > 64)
        {
            throw new InventoryDomainException("业务请求号长度不能超过 64 个字符");
        }

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

        await inventoryRepository.ChangeAsync(increases, decreases, input.RequestNo, cancellationToken);
    }
}
