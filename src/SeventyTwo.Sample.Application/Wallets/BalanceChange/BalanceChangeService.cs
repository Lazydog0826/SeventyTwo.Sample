using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

[AutofacDependency(typeof(IBalanceChangeService))]
public class BalanceChangeService(IWalletRepository walletRepository, IUnitOfWork unitOfWork) : IBalanceChangeService
{
    public Task BalanceChangeAsync(BalanceChangeDraft draft, CancellationToken cancellationToken)
    {
        List<BalanceChangeDetailDraft> balanceChangeDrafts =
        [
            .. draft
                .Drafts.GroupBy(x => new { x.Currency, x.ChangeType })
                .Select(x => new BalanceChangeDetailDraft(x.Key.Currency, x.Key.ChangeType, x.Sum(x2 => x2.Amount)))
                .OrderBy(x => x.ChangeType),
        ];

        var currencyList = balanceChangeDrafts.Select(x => x.Currency).ToList();

        return unitOfWork.ExecuteAsync(
            async () =>
            {
                var registered = await walletRepository.TryRegisterBalanceChangeAsync(
                    draft.RequestNo,
                    cancellationToken
                );
                if (!registered)
                {
                    return;
                }

                var walletList = await walletRepository.GetForBalanceChangeAsync(
                    draft.CustomerId,
                    currencyList,
                    cancellationToken
                );
                var addWalletList = new List<Wallet>();
                var updateWalletList = new List<Wallet>();
                var addWalletChangeRecordList = new List<WalletChangeRecordDraft>();
                var pendingCurWallet = new HashSet<WalletCurrency>();
                var walletDic = walletList.ToDictionary(x => x.Currency, x => x);

                balanceChangeDrafts.ForEach(x =>
                {
                    if (walletDic.TryGetValue(x.Currency, out var wallet))
                    {
                        if (!pendingCurWallet.Contains(x.Currency))
                        {
                            updateWalletList.Add(wallet);
                            pendingCurWallet.Add(x.Currency);
                        }
                    }
                    else
                    {
                        wallet = new Wallet(Yitter.IdGenerator.YitIdHelper.NextId(), draft.CustomerId, x.Currency, 0)
                        {
                            CreatedBy = 0,
                            CreatedAt = DateTimeExtension.Now(),
                        };
                        if (!pendingCurWallet.Contains(x.Currency))
                        {
                            addWalletList.Add(wallet);
                            walletDic.Add(x.Currency, wallet);
                            pendingCurWallet.Add(x.Currency);
                        }
                    }

                    var (oldBalanceAmount, newBalanceAmount) = wallet.ChangeBalance(x.Amount, x.ChangeType);

                    addWalletChangeRecordList.Add(
                        new WalletChangeRecordDraft
                        {
                            ChangeId = Yitter.IdGenerator.YitIdHelper.NextId(),
                            RequestNo = draft.RequestNo,
                            WalletId = wallet.Id,
                            ChangeType = x.ChangeType,
                            Amount = x.Amount,
                            BeforeBalanceAmount = oldBalanceAmount,
                            AfterBalanceAmount = newBalanceAmount,
                            ChangedAt = DateTimeExtension.Now(),
                        }
                    );
                });

                await walletRepository.SaveBalanceChangeAsync(
                    addWalletList,
                    updateWalletList,
                    addWalletChangeRecordList,
                    cancellationToken
                );
            },
            cancellationToken
        );
    }
}
