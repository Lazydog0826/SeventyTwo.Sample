using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Inventories;
using SqlSugar;

// ReSharper disable InvertIf

// ReSharper disable MemberCanBeMadeStatic.Local

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

    public async Task EnsureChangeLocksAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var orderKeys = OrderBy(keys);
        await InventoryLockInitializer.EnsureCreatedAsync(db, orderKeys, cancellationToken);
    }

    public async Task<IReadOnlyList<Inventory>> GetForChangeAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken
    )
    {
        var orderKeys = OrderBy(keys);

        // 需要扣减时才上锁和查询，纯新增不需要
        if (orderKeys.Any())
        {
            var lockedKeys = await db.Queryable<InventoryChangeLock>()
                .Where(x => orderKeys.Contains(x.LockKey))
                .OrderBy(x => x.LockKey)
                .TranLock(DbLockType.Wait)
                .Select(x => x.LockKey)
                .ToListAsync(cancellationToken);

            if (lockedKeys.Count != orderKeys.Count || !lockedKeys.ToHashSet().SetEquals(orderKeys))
            {
                throw new InvalidOperationException("库存维度锁获取不完整");
            }

            var records = await db.Queryable<InventoryRecord>()
                .Where(x => orderKeys.Contains(x.Key) && x.Quantity > 0)
                .OrderBy(x => x.Key)
                .ToListAsync(cancellationToken);
            return records.Adapt<List<Inventory>>();
        }

        return [];
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

    /// <summary>
    /// 固定排序规则
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    private static List<string> OrderBy(IReadOnlyCollection<string> keys)
    {
        return [.. keys.OrderBy(x => x, StringComparer.Ordinal)];
    }
}
