using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Wallets;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

[AutofacDependency(typeof(IWalletRepository))]
public class WalletRepository(ISqlSugarClient db) : IWalletRepository
{
    public async Task BalanceChangeAsync(BalanceChangeDraft draft, CancellationToken cancellationToken)
    {
        if (!draft.Drafts.Any())
        {
            return;
        }

        List<BalanceChangeDetailDraft> balanceChangeDrafts =
        [
            .. draft
                .Drafts.GroupBy(x => new { x.Currency, x.ChangeType })
                .Select(x => new BalanceChangeDetailDraft(x.Key.Currency, x.Key.ChangeType, x.Sum(x2 => x2.Amount)))
                .OrderBy(x => x.ChangeType),
        ];

        var key = draft.CustomerId.ToString();
        var currencyList = balanceChangeDrafts.Select(x => x.Currency).ToList();

        var addWalletList = new List<WalletRecord>();
        var updateWalletList = new List<WalletRecord>();
        var addWalletChangeRecordList = new List<WalletChangeRecord>();

        using var uow = db.CreateContext(db.Ado.IsNoTran());

        #region 幂等

        var newRequest = new WalletChangeRequest() { RequestNo = draft.RequestNo, RequestAt = DateTimeExtension.Now() };
        var addRequestResult = await db.Insertable(newRequest)
            .PostgreSQLConflictNothing(["request_no"])
            .ExecuteCommandAsync(cancellationToken);

        if (addRequestResult == 0)
        {
            // 抛出异常或直接返回
            return;
        }

        #endregion

        #region 防并发

        var newLock = new WalletChangeLock { LockKey = key };
        await db.Insertable(newLock).PostgreSQLConflictNothing(["lock_key"]).ExecuteCommandAsync(cancellationToken);
        await db.Queryable<WalletChangeLock>()
            .Where(x => x.LockKey == key)
            .TranLock(DbLockType.Wait)
            .FirstAsync(cancellationToken);

        #endregion

        #region 操作

        var walletList = await db.Queryable<WalletRecord>()
            .Where(x => x.CustomerId == draft.CustomerId && currencyList.Contains(x.Currency))
            .ToListAsync(cancellationToken);

        var pendingCurWallet = new HashSet<WalletCurrency>();
        var walletDic = walletList.ToDictionary(x => x.Currency, x => x);

        balanceChangeDrafts.ForEach(x =>
        {
            decimal oldBalanceAmount;
            decimal newBalanceAmount;

            if (walletDic.TryGetValue(x.Currency, out var temWallet))
            {
                if (!pendingCurWallet.Contains(x.Currency))
                {
                    updateWalletList.Add(temWallet);
                    pendingCurWallet.Add(x.Currency);
                }
            }
            else
            {
                temWallet = new WalletRecord
                {
                    Id = Yitter.IdGenerator.YitIdHelper.NextId(),
                    CreatedBy = 0,
                    CreatedAt = DateTimeExtension.Now(),
                    CustomerId = draft.CustomerId,
                    Currency = x.Currency,
                    BalanceAmount = 0,
                };
                if (!pendingCurWallet.Contains(x.Currency))
                {
                    addWalletList.Add(temWallet);
                    walletDic.Add(x.Currency, temWallet);
                    pendingCurWallet.Add(x.Currency);
                }
            }

            switch (x.ChangeType)
            {
                case WalletChangeType.Increase:
                    oldBalanceAmount = temWallet.BalanceAmount;
                    temWallet.BalanceAmount += x.Amount;
                    newBalanceAmount = temWallet.BalanceAmount;
                    break;
                case WalletChangeType.Decrease:
                    oldBalanceAmount = temWallet.BalanceAmount;
                    temWallet.BalanceAmount -= x.Amount;
                    newBalanceAmount = temWallet.BalanceAmount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(x.ChangeType));
            }

            if (temWallet.BalanceAmount < 0)
            {
                throw new WalletDomainException("余额不足");
            }

            addWalletChangeRecordList.Add(
                new WalletChangeRecord
                {
                    ChangeId = Yitter.IdGenerator.YitIdHelper.NextId(),
                    RequestNo = draft.RequestNo,
                    WalletId = temWallet.Id,
                    ChangeType = x.ChangeType,
                    Amount = x.Amount,
                    BeforeBalanceAmount = oldBalanceAmount,
                    AfterBalanceAmount = newBalanceAmount,
                    ChangedAt = DateTimeExtension.Now(),
                }
            );
        });

        #endregion

        if (addWalletList.Any())
        {
            await db.Insertable(addWalletList).ExecuteCommandAsync(cancellationToken);
        }

        if (updateWalletList.Any())
        {
            await db.Updateable(updateWalletList).ExecuteCommandAsync(cancellationToken);
        }

        if (addWalletChangeRecordList.Any())
        {
            await db.Insertable(addWalletChangeRecordList).ExecuteCommandAsync(cancellationToken);
        }

        uow.Commit();
    }
}
