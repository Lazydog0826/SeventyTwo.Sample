using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Inventories;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Inventories;

[AutofacDependency(typeof(IInventoryRepository))]
public sealed class InventoryRepository(ISqlSugarClient db) : IInventoryRepository
{
    public async Task<InventoryChange> ChangeAsync(InventoryChangeDraft change, CancellationToken cancellationToken)
    {
        InventoryChange? completedChange = null;
        var transaction = await db.Ado.UseTranAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var changeRecord = new InventoryChangeRecord
            {
                ChangeId = change.Id,
                RequestNo = change.RequestNo,
                InventoryId = change.InventoryId,
                ChangeType = (short)change.ChangeType,
                Quantity = change.Quantity,
                ChangedAt = change.ChangedAt,
            };
            var inserted = await db.Insertable(changeRecord)
                .PostgreSQLConflictNothing(["request_no"])
                .ExecuteCommandAsync(cancellationToken);

            if (inserted == 0)
            {
                var existing = await GetByRequestNoAsync(change.RequestNo);
                if (existing is null)
                {
                    throw new InvalidOperationException("读取库存幂等记录失败");
                }

                if (
                    existing.InventoryId != change.InventoryId
                    || existing.ChangeType != (short)change.ChangeType
                    || existing.Quantity != change.Quantity
                )
                {
                    throw new InventoryDomainException("业务请求号已用于其他库存变更");
                }

                completedChange = ToDomain(existing);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var inventoryRecord = await db.Queryable<InventoryRecord>()
                .Where(record => record.Id == change.InventoryId)
                .TranLock(DbLockType.Wait)
                .FirstAsync(cancellationToken);
            if (inventoryRecord is null)
            {
                throw new InventoryDomainException("库存不存在");
            }

            var inventory = ToDomain(inventoryRecord);
            var quantityChange = change.ChangeType switch
            {
                InventoryChangeType.Increase => inventory.Increase(change.Quantity),
                InventoryChangeType.Decrease => inventory.Decrease(change.Quantity),
                _ => throw new InventoryDomainException("库存变更类型无效"),
            };

            var updatedInventory = await db.Updateable<InventoryRecord>()
                .SetColumns(record => record.Quantity == inventory.Quantity)
                .Where(record => record.Id == inventory.Id)
                .ExecuteCommandAsync(cancellationToken);
            if (updatedInventory != 1)
            {
                throw new InvalidOperationException("更新库存失败");
            }

            var updatedChange = await db.Updateable<InventoryChangeRecord>()
                .SetColumns(record => new InventoryChangeRecord
                {
                    BeforeQuantity = quantityChange.BeforeQuantity,
                    AfterQuantity = quantityChange.AfterQuantity,
                })
                .Where(record => record.ChangeId == change.Id)
                .ExecuteCommandAsync(cancellationToken);
            if (updatedChange != 1)
            {
                throw new InvalidOperationException("更新库存变更记录失败");
            }

            completedChange = new InventoryChange(
                change.Id,
                change.RequestNo,
                change.InventoryId,
                change.ChangeType,
                change.Quantity,
                quantityChange.BeforeQuantity,
                quantityChange.AfterQuantity,
                change.ChangedAt
            );
        });

        if (!transaction.IsSuccess)
        {
            throw new InvalidOperationException("库存变更失败", transaction.ErrorException);
        }

        return completedChange ?? throw new InvalidOperationException("库存变更结果为空");
    }

    private async Task<InventoryChangeRecord?> GetByRequestNoAsync(string requestNo)
    {
        return await db.Queryable<InventoryChangeRecord>().Where(record => record.RequestNo == requestNo).FirstAsync();
    }

    private Inventory ToDomain(InventoryRecord record)
    {
        var inventory = new Inventory(
            record.Id,
            record.ProductId,
            record.WarehouseId,
            record.LocationId,
            record.InboundBatchNo,
            record.InboundAt,
            record.InitialQuantity,
            record.Quantity
        );
        inventory.EntityToAggregateRoot(record);
        return inventory;
    }

    private InventoryChange ToDomain(InventoryChangeRecord record)
    {
        return new InventoryChange(
            record.ChangeId,
            record.RequestNo,
            record.InventoryId,
            (InventoryChangeType)record.ChangeType,
            record.Quantity,
            record.BeforeQuantity,
            record.AfterQuantity,
            record.ChangedAt
        );
    }
}
