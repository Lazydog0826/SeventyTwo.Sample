using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Inventories;
using SqlSugar;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SeventyTwo.Sample.Infrastructure.Inventories;

[AutofacDependency(typeof(IInventoryRepository))]
public sealed class InventoryRepository(ISqlSugarClient db) : IInventoryRepository
{
    public async Task<bool> TryRegisterChangeAsync(string requestNo, CancellationToken cancellationToken)
    {
        var newRequest = new InventoryChangeRequest { RequestNo = requestNo, RequestAt = DateTimeExtension.Now() };
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        var affectedRows = await (
            db.CurrentConnectionConfig.DbType switch
            {
                DbType.PostgreSQL => db.Insertable(newRequest)
                    .PostgreSQLConflictNothing(["request_no"])
                    .ExecuteCommandAsync(cancellationToken),
                DbType.MySql => db.Insertable(newRequest).MySqlIgnore().ExecuteCommandAsync(cancellationToken),
                DbType.SqlServer => db.Ado.ExecuteCommandAsync(
                    """
                    if not exists (
                        select 1
                        from inventory_change_request with (updlock, holdlock)
                        where request_no = @RequestNo
                    )
                    begin
                        insert into inventory_change_request (request_no, request_at)
                        values (@RequestNo, @RequestAt);
                    end;
                    """,
                    new[]
                    {
                        new SugarParameter("@RequestNo", newRequest.RequestNo, System.Data.DbType.AnsiString)
                        {
                            Size = 26,
                        },
                        new SugarParameter("@RequestAt", newRequest.RequestAt),
                    },
                    cancellationToken
                ),
                _ => throw new NotSupportedException($"不支持的数据库类型：{db.CurrentConnectionConfig.DbType}"),
            }
        );
        return affectedRows > 0;
    }

    public async Task EnsureChangeLocksAsync(
        IReadOnlyCollection<InventoryDimension> dimensions,
        CancellationToken cancellationToken
    )
    {
        var locks = dimensions
            .Select(GetKey)
            .Distinct()
            .OrderBy(x => x, StringComparer.Ordinal)
            .Select(x => new InventoryChangeLock { LockKey = x })
            .ToList();
        await InventoryLockInitializer.EnsureCreatedAsync(db, locks, cancellationToken);
    }

    public async Task<IReadOnlyList<Inventory>> GetForChangeAsync(
        IReadOnlyCollection<InventoryDimension> dimensions,
        CancellationToken cancellationToken
    )
    {
        var keys = dimensions.Select(GetKey).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
        var lockedKeys = await db.Queryable<InventoryChangeLock>()
            .Where(x => keys.Contains(x.LockKey))
            .OrderBy(x => x.LockKey)
            .TranLock(DbLockType.Wait)
            .Select(x => x.LockKey)
            .ToListAsync(cancellationToken);

        if (lockedKeys.Count != keys.Count || !lockedKeys.ToHashSet().SetEquals(keys))
        {
            throw new InvalidOperationException("库存维度锁获取不完整");
        }

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
