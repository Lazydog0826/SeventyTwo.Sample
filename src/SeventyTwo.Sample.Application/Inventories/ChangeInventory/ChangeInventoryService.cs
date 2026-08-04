using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Application.Inventories.ChangeInventory;

[AutofacDependency(typeof(IChangeInventoryService))]
public sealed class ChangeInventoryService(IInventoryRepository inventoryRepository, IUnitOfWork unitOfWork)
    : IChangeInventoryService
{
    private readonly InventoryChangeService _inventoryChangeService = new();

    public Task ChangeAsync(InventoryChangeDraft draft, CancellationToken cancellationToken)
    {
        if (draft.Increases.Count == 0 && draft.Decreases.Count == 0)
        {
            return Task.CompletedTask;
        }

        var dimensions = draft
            .Increases.Cast<InventoryDraft>()
            .Concat(draft.Decreases)
            .Select(x => new InventoryDimension(x.ProductId, x.WarehouseId, x.LocationId))
            .Distinct()
            .ToList();

        return unitOfWork.ExecuteAsync(
            async () =>
            {
                var registered = await inventoryRepository.TryRegisterChangeAsync(draft.RequestNo, cancellationToken);
                if (!registered)
                {
                    return;
                }

                var inventories = await inventoryRepository.GetForChangeAsync(dimensions, cancellationToken);
                var changedAt = DateTimeExtension.Now();
                var batch = _inventoryChangeService.Change(inventories, draft, Guid.CreateVersion7, changedAt);

                foreach (var inventory in batch.NewInventories)
                {
                    inventory.CreatedBy = SystemIds.System;
                    inventory.CreatedAt = changedAt;
                }

                await inventoryRepository.SaveChangeAsync(
                    draft.RequestNo,
                    batch.NewInventories,
                    batch.ChangedInventories,
                    batch.Changes,
                    cancellationToken
                );
            },
            cancellationToken
        );
    }
}
