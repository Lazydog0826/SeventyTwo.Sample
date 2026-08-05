using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Wallets;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

[AutofacDependency(typeof(IWalletRepository))]
public sealed class WalletRepository(ISqlSugarClient db) : IWalletRepository
{
    public async Task<bool> TryRegisterBalanceChangeAsync(string requestNo, CancellationToken cancellationToken)
    {
        var newRequest = new WalletChangeRequest { RequestNo = requestNo, RequestAt = DateTimeExtension.Now() };
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
                        from wallet_change_request with (updlock, holdlock)
                        where request_no = @RequestNo
                    )
                    begin
                        insert into wallet_change_request (request_no, request_at)
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

    public async Task EnsureChangeLocksAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var orderKeys = OrderBy(keys);
        await WalletLockInitializer.EnsureCreatedAsync(db, orderKeys, cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetForBalanceChangeAsync(
        string customerId,
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
                    ChangeId = Ulid.NewUlid().ToString(),
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
