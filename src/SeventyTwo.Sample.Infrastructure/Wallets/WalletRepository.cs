using AutoMapper;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Wallets;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

[AutofacDependency(typeof(IWalletRepository))]
public class WalletRepository(ISqlSugarClient db, IMapper mapper) : IWalletRepository
{
    private readonly ISqlSugarClient _db = db;

    public async Task<bool> TryRegisterBalanceChangeAsync(string requestNo, CancellationToken cancellationToken)
    {
        var newRequest = new WalletChangeRequest() { RequestNo = requestNo, RequestAt = DateTimeExtension.Now() };
        var affectedRows = await _db.Insertable(newRequest)
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
        await _db.Insertable(newLock).PostgreSQLConflictNothing(["lock_key"]).ExecuteCommandAsync(cancellationToken);
        await _db.Queryable<WalletChangeLock>()
            .Where(x => x.LockKey == key)
            .TranLock(DbLockType.Wait)
            .FirstAsync(cancellationToken);

        var dbDataList = await _db.Queryable<WalletRecord>()
            .Where(x => x.CustomerId == customerId && walletCurrencies.Contains(x.Currency))
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Wallet>>(dbDataList);
    }

    public async Task SaveBalanceChangeAsync(
        IReadOnlyCollection<Wallet> newWallets,
        IReadOnlyCollection<Wallet> changedWallets,
        IReadOnlyCollection<WalletChangeRecordDraft> changeRecords,
        CancellationToken cancellationToken
    )
    {
        if (newWallets.Count > 0)
        {
            var newWalletRecords = mapper.Map<List<WalletRecord>>(newWallets);
            await _db.Insertable(newWalletRecords).ExecuteCommandAsync(cancellationToken);
        }

        if (changedWallets.Count > 0)
        {
            var changedWalletRecords = mapper.Map<List<WalletRecord>>(changedWallets);
            await _db.Updateable(changedWalletRecords).ExecuteCommandAsync(cancellationToken);
        }

        if (changeRecords.Count > 0)
        {
            var changeRecordEntities = mapper.Map<List<WalletChangeRecord>>(changeRecords);
            await _db.Insertable(changeRecordEntities).ExecuteCommandAsync(cancellationToken);
        }
    }
}
