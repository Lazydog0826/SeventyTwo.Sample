using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

[AutofacDependency(typeof(IBalanceChangeService))]
public class BalanceChangeService(IWalletRepository walletRepository, IUnitOfWork unitOfWork) : IBalanceChangeService
{
    private readonly WalletBalanceChangeService _walletBalanceChangeService = new();

    public Task BalanceChangeAsync(BalanceChangeCommand command, CancellationToken cancellationToken)
    {
        var walletTypes = command.Details.Select(x => x.WalletType).Distinct().ToList();
        var requests = command
            .Details.Select(x => new WalletBalanceChangeRequest(x.WalletType, x.ChangeType, x.Amount))
            .ToList();

        return unitOfWork.ExecuteAsync(
            async () =>
            {
                var registered = await walletRepository.TryRegisterBalanceChangeAsync(
                    command.RequestNo,
                    cancellationToken
                );
                if (!registered)
                {
                    return;
                }

                var walletList = await walletRepository.GetForBalanceChangeAsync(
                    command.CustomerId,
                    walletTypes,
                    cancellationToken
                );

                var batch = _walletBalanceChangeService.Change(
                    command.CustomerId,
                    walletList,
                    requests,
                    Guid.CreateVersion7
                );
                var createdAt = DateTimeExtension.Now();
                foreach (var wallet in batch.NewWallets)
                {
                    wallet.CreatedBy = SystemIds.System;
                    wallet.CreatedAt = createdAt;
                }

                await walletRepository.SaveBalanceChangeAsync(
                    command.RequestNo,
                    batch.NewWallets,
                    batch.ChangedWallets,
                    batch.Changes,
                    cancellationToken
                );
            },
            cancellationToken
        );
    }
}
