using Dm.util;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Inventories;
using SqlSugar;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SeventyTwo.Sample.Infrastructure.Inventories;

[AutofacDependency(typeof(IInventoryRepository))]
public sealed class InventoryRepository(ISqlSugarClient db) : IInventoryRepository
{
    public async Task ChangeAsync(
        List<InventoryIncreaseDraft> increases,
        List<InventoryDecreaseDraft> drafts,
        string requestNo,
        CancellationToken cancellationToken
    )
    {
        if (increases.Any(x => x.Quantity <= 0) || drafts.Any(x => x.Quantity <= 0))
        {
            throw new InventoryDomainException("库存变更数量必须大于 0");
        }

        if (!increases.Any() && !drafts.Any())
        {
            return;
        }

        drafts =
        [
            .. drafts
                .GroupBy(x => new
                {
                    x.WarehouseId,
                    x.LocationId,
                    x.ProductId,
                })
                .Select(x => new InventoryDecreaseDraft(
                    x.Key.ProductId,
                    x.Key.WarehouseId,
                    x.Key.LocationId,
                    x.Sum(x2 => x2.Quantity)
                )),
        ];

        var increaseKeys = increases.Select(GetKey);
        var draftKeys = drafts.Select(GetKey);
        var allKeys = increaseKeys.Concat(draftKeys).Distinct().OrderBy(x => x).ToList();

        using var uow = db.CreateContext(db.Ado.IsNoTran());

        #region 幂等

        var newRequest = new InventoryChangeRequest { RequestNo = requestNo, RequestAt = DateTimeExtension.Now() };

        var addRequestResult = await db.Insertable(newRequest)
            .PostgreSQLConflictNothing(["request_no"])
            .ExecuteCommandAsync(cancellationToken);

        if (addRequestResult == 0)
        {
            // 抛出异常或直接返回
            return;
        }

        #endregion

        #region 维度锁防并发

        var lockList = allKeys.Select(x => new InventoryChangeLock { LockKey = x }).ToList();
        await db.Insertable(lockList).PostgreSQLConflictNothing(["lock_key"]).ExecuteCommandAsync(cancellationToken);

        _ = await db.Queryable<InventoryChangeLock>()
            .Where(x => allKeys.Contains(x.LockKey))
            .OrderBy(x => x.LockKey)
            .TranLock(DbLockType.Wait)
            .ToListAsync(cancellationToken);

        #endregion

        #region 库存操作

        var inventoryList = await db.Queryable<InventoryRecord>()
            .Where(x => allKeys.Contains(x.Key) && x.Quantity > 0)
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);

        var inventoryChangeRecordList = new List<InventoryChangeRecord>();
        var updateInventoryList = new List<InventoryRecord>();
        var addInventoryList = new List<InventoryRecord>();

        increases.ForEach(x =>
        {
            var inventoryId = Yitter.IdGenerator.YitIdHelper.NextId();
            addInventoryList.add(
                new InventoryRecord
                {
                    Id = inventoryId,
                    CreatedBy = 0,
                    CreatedAt = DateTimeExtension.Now(),
                    Key = GetKey(x),
                    ProductId = x.ProductId,
                    WarehouseId = x.WarehouseId,
                    LocationId = x.LocationId,
                    InboundBatchNo = x.InboundBatchNo,
                    InboundAt = x.ChangedAt,
                    InitialQuantity = x.Quantity,
                    Quantity = x.Quantity,
                }
            );
            inventoryChangeRecordList.Add(
                new InventoryChangeRecord
                {
                    ChangeId = Yitter.IdGenerator.YitIdHelper.NextId(),
                    RequestNo = requestNo,
                    InventoryId = inventoryId,
                    ChangeType = InventoryChangeType.Increase,
                    Quantity = x.Quantity,
                    BeforeQuantity = 0,
                    AfterQuantity = x.Quantity,
                    ChangedAt = x.ChangedAt,
                }
            );
        });

        drafts.ForEach(x =>
        {
            var draftQty = x.Quantity;

            var currKey = GetKey(x);
            var currInventoryList = inventoryList.Where(x2 => x2.Key == currKey && x2.Quantity > 0).ToList();
            currInventoryList.AddRange([.. addInventoryList.Where(x2 => x2.Key == currKey && x2.Quantity > 0)]);
            currInventoryList = [.. currInventoryList.OrderByDescending(x2 => x2.InboundAt)];

            while (currInventoryList.Any())
            {
                var first = currInventoryList.First();
                int oldQty;
                int newQty;

                if (first.Quantity > draftQty)
                {
                    oldQty = first.Quantity;
                    newQty = first.Quantity - draftQty;
                    draftQty = 0;
                }
                else
                {
                    oldQty = first.Quantity;
                    newQty = 0;
                    draftQty -= first.Quantity;
                }

                first.Quantity = newQty;

                inventoryChangeRecordList.Add(
                    new InventoryChangeRecord
                    {
                        ChangeId = Yitter.IdGenerator.YitIdHelper.NextId(),
                        RequestNo = requestNo,
                        InventoryId = first.Id,
                        ChangeType = InventoryChangeType.Decrease,
                        Quantity = oldQty - newQty,
                        BeforeQuantity = oldQty,
                        AfterQuantity = newQty,
                        ChangedAt = DateTimeExtension.Now(),
                    }
                );

                currInventoryList.Remove(first);
                updateInventoryList.Add(first);
                if (draftQty == 0)
                {
                    break;
                }
            }

            if (draftQty > 0)
            {
                throw new InventoryDomainException("库存不足");
            }
        });

        if (addInventoryList.Any())
        {
            await db.Insertable(addInventoryList).ExecuteCommandAsync(cancellationToken);
        }

        if (updateInventoryList.Any())
        {
            await db.Updateable(updateInventoryList).ExecuteCommandAsync(cancellationToken);
        }

        if (inventoryChangeRecordList.Any())
        {
            await db.Insertable(inventoryChangeRecordList).ExecuteCommandAsync(cancellationToken);
        }

        #endregion

        uow.Commit();
    }

    private string GetKey(InventoryDraft increase)
    {
        return $"{increase.WarehouseId}:{increase.LocationId}:{increase.ProductId}";
    }
}
