using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Wallets;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

[AutofacDependency(typeof(IWalletRepository))]
public sealed class WalletRepository(ISqlSugarClient db) : IWalletRepository
{
    public async Task<bool> TryRegisterBalanceChangeAsync(Guid requestNo, CancellationToken cancellationToken)
    {
        var newRequest = new WalletChangeRequest { RequestNo = requestNo, RequestAt = DateTimeExtension.Now() };
        var affectedRows = await db.Insertable(newRequest)
            .PostgreSQLConflictNothing(["request_no"])
            .ExecuteCommandAsync(cancellationToken);
        return affectedRows > 0;
    }

    public Task EnsureChangeLocksAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var orderKeys = OrderBy(keys);
        var locks = orderKeys.Select(x => new WalletChangeLock { LockKey = x }).ToList();
        return db.Insertable(locks).PostgreSQLConflictNothing(["lock_key"]).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetForBalanceChangeAsync(
        Guid customerId,
        IReadOnlyCollection<WalletCurrency> walletCurrencies,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken
    )
    {
        var orderKeys = OrderBy(keys);

        if (orderKeys.Any())
        {
            var lockedKeys = await db.Queryable<WalletChangeLock>()
                .Where(x => orderKeys.Contains(x.LockKey))
                .OrderBy(x => x.LockKey)
                .TranLock(DbLockType.Wait)
                .Select(x => x.LockKey)
                .ToListAsync(cancellationToken);

            if (lockedKeys.Count != orderKeys.Count || !lockedKeys.ToHashSet().SetEquals(orderKeys))
            {
                throw new InvalidOperationException("钱包客户维度锁获取不完整");
            }
        }

        var dbDataList = await db.Queryable<WalletRecord>()
            .Where(x => x.CustomerId == customerId && walletCurrencies.Contains(x.Currency))
            .ToListAsync(cancellationToken);
        return dbDataList.Adapt<List<Wallet>>();
    }

    public async Task SaveBalanceChangeAsync(
        Guid requestNo,
        IReadOnlyCollection<Wallet> newWallets,
        IReadOnlyCollection<Wallet> changedWallets,
        IReadOnlyCollection<WalletBalanceChange> changes,
        CancellationToken cancellationToken
    )
    {
        if (newWallets.Count > 0)
        {
            var newWalletRecords = newWallets.Adapt<List<WalletRecord>>();
            await db.Insertable(newWalletRecords).ExecuteCommandAsync(cancellationToken);
        }

        if (changedWallets.Count > 0)
        {
            // 规范（设计取舍）：余额变更不回写钱包主表的 updated_by/updated_at（聚合仅修改 Balance），
            // 本方法按全字段更新会原样写回旧值；"谁在何时改了余额"的审计以 wallet_change_record
            // 变更明细表为准。若未来需要主表审计，须由调用方在聚合上设置 UpdatedBy/UpdatedAt
            // 并改为 UpdateColumns 精确更新，避免覆盖并发写入。
            var changedWalletRecords = changedWallets.Adapt<List<WalletRecord>>();
            await db.Updateable(changedWalletRecords).ExecuteCommandAsync(cancellationToken);
        }

        if (changes.Count > 0)
        {
            var changedAt = DateTimeExtension.Now();
            var changeRecordEntities = changes
                .Select(x => new WalletChangeRecord
                {
                    ChangeId = Guid.CreateVersion7(),
                    RequestNo = requestNo,
                    WalletId = x.WalletId,
                    ChangeType = x.ChangeType,
                    Amount = x.Amount.Value,
                    BeforeBalanceAmount = x.BeforeBalance.Value,
                    AfterBalanceAmount = x.AfterBalance.Value,
                    ChangedAt = changedAt,
                })
                .ToList();
            await db.Insertable(changeRecordEntities).ExecuteCommandAsync(cancellationToken);
        }
    }

    private static List<string> OrderBy(IReadOnlyCollection<string> keys)
    {
        return [.. keys.OrderBy(x => x, StringComparer.Ordinal)];
    }
}
