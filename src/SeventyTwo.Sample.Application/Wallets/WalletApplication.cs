using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;

namespace SeventyTwo.Sample.Application.Wallets;

[AutofacDependency(typeof(IWalletApplication))]
public sealed class WalletApplication(IBalanceChangeService balanceChangeService) : IWalletApplication
{
    public Task BalanceChangeAsync(BalanceChangeInput input, CancellationToken cancellationToken)
    {
        return balanceChangeService.BalanceChangeAsync(input.Adapt<BalanceChangeCommand>(), cancellationToken);
    }
}
