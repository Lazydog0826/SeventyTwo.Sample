using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets;

[AutofacDependency(typeof(IWalletApplication))]
public sealed class WalletApplication(IBalanceChangeService balanceChangeService) : IWalletApplication
{
    public Task BalanceChangeAsync(BalanceChangeInput input, CancellationToken cancellationToken)
    {
        var details = input
            .Details.Select(x => new BalanceChangeDetailCommand(x.Currency, x.ChangeType, new Money(x.Amount)))
            .ToList();
        var command = new BalanceChangeCommand(input.CustomerId, input.RequestNo, details);

        return balanceChangeService.BalanceChangeAsync(command, cancellationToken);
    }
}
