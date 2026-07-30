using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Wallets;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

[AutofacDependency(typeof(IWalletRepository))]
public class WalletRepository(ISqlSugarClient db) : IWalletRepository
{
    public async Task<bool> TryRegisterBalanceChangeAsync(string requestNo, CancellationToken cancellationToken)
    {
        var newRequest = new WalletChangeRequest() { RequestNo = requestNo, RequestAt = DateTimeExtension.Now() };
        var affectedRows = await db.Insertable(newRequest)
            .PostgreSQLConflictNothing(["request_no"])
            .ExecuteCommandAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<Wallet>> GetForBalanceChangeAsync(
        long customerId,
        IReadOnlyCollection<WalletCurrency> walletCurrencies,
        CancellationToken cancellationToken
    )
    {
        var key = customerId.ToString();
        var newLock = new WalletChangeLock { LockKey = key };
        await db.Insertable(newLock).PostgreSQLConflictNothing(["lock_key"]).ExecuteCommandAsync(cancellationToken);
        await db.Queryable<WalletChangeLock>()
            .Where(x => x.LockKey == key)
            .TranLock(DbLockType.Wait)
            .FirstAsync(cancellationToken);

        var dbDataList = await db.Queryable<WalletRecord>()
            .Where(x => x.CustomerId == customerId && walletCurrencies.Contains(x.Currency))
            .ToListAsync(cancellationToken);
        return dbDataList.Adapt<List<Wallet>>();
    }

    public async Task SaveBalanceChangeAsync(
        string requestNo,
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
            var changedWalletRecords = changedWallets.Adapt<List<WalletRecord>>();
            await db.Updateable(changedWalletRecords).ExecuteCommandAsync(cancellationToken);
        }

        if (changes.Count > 0)
        {
            var changedAt = DateTimeExtension.Now();
            var changeRecordEntities = changes
                .Select(x => new WalletChangeRecord
                {
                    ChangeId = Yitter.IdGenerator.YitIdHelper.NextId(),
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
}
