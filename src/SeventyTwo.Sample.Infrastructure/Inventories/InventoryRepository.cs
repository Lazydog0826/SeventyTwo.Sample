using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Inventories;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Inventories;

[AutofacDependency(typeof(IInventoryRepository))]
public sealed class InventoryRepository(ISqlSugarClient db) : IInventoryRepository
{
    public async Task<bool> TryRegisterChangeAsync(string requestNo, CancellationToken cancellationToken)
    {
        var newRequest = new InventoryChangeRequest { RequestNo = requestNo, RequestAt = DateTimeExtension.Now() };
        var affectedRows = await db.Insertable(newRequest)
            .PostgreSQLConflictNothing(["request_no"])
            .ExecuteCommandAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<Inventory>> GetForChangeAsync(
        IReadOnlyCollection<InventoryDimension> dimensions,
        CancellationToken cancellationToken
    )
    {
        var keys = dimensions.Select(GetKey).Distinct().OrderBy(x => x).ToList();
        var locks = keys.Select(x => new InventoryChangeLock { LockKey = x }).ToList();
        await db.Insertable(locks).PostgreSQLConflictNothing(["lock_key"]).ExecuteCommandAsync(cancellationToken);
        await db.Queryable<InventoryChangeLock>()
            .Where(x => keys.Contains(x.LockKey))
            .OrderBy(x => x.LockKey)
            .TranLock(DbLockType.Wait)
            .ToListAsync(cancellationToken);

        var records = await db.Queryable<InventoryRecord>()
            .Where(x => keys.Contains(x.Key) && x.Quantity > 0)
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);
        return records.Adapt<List<Inventory>>();
    }

    public async Task SaveChangeAsync(
        string requestNo,
        IReadOnlyCollection<Inventory> newInventories,
        IReadOnlyCollection<Inventory> changedInventories,
        IReadOnlyCollection<InventoryChange> changes,
        CancellationToken cancellationToken
    )
    {
        if (newInventories.Count > 0)
        {
            var newRecords = newInventories.Adapt<List<InventoryRecord>>();
            await db.Insertable(newRecords).ExecuteCommandAsync(cancellationToken);
        }

        if (changedInventories.Count > 0)
        {
            var changedRecords = changedInventories.Adapt<List<InventoryRecord>>();
            await db.Updateable(changedRecords).ExecuteCommandAsync(cancellationToken);
        }

        if (changes.Count > 0)
        {
            var changeRecords = changes
                .Select(x => new InventoryChangeRecord
                {
                    ChangeId = Ulid.NewUlid().ToString(),
                    RequestNo = requestNo,
                    InventoryId = x.InventoryId,
                    ChangeType = x.ChangeType,
                    Quantity = x.Quantity,
                    BeforeQuantity = x.BeforeQuantity,
                    AfterQuantity = x.AfterQuantity,
                    ChangedAt = x.ChangedAt,
                })
                .ToList();
            await db.Insertable(changeRecords).ExecuteCommandAsync(cancellationToken);
        }
    }

    private string GetKey(InventoryDimension dimension)
    {
        return $"{dimension.WarehouseId}:{dimension.LocationId}:{dimension.ProductId}";
    }
}
