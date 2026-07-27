using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets;

[AutofacDependency(typeof(IWalletApplication))]
public sealed class WalletApplication(IWalletRepository walletRepository) : IWalletApplication
{
    public Task BalanceChangeAsync(BalanceChangeInput input, CancellationToken cancellationToken)
    {
        var details = input
            .Details.Select(x => new BalanceChangeDetailDraft(x.Currency, x.ChangeType, x.Amount))
            .ToList();
        var draft = new BalanceChangeDraft(input.CustomerId, input.RequestNo, details);

        return walletRepository.BalanceChangeAsync(draft, cancellationToken);
    }
}
