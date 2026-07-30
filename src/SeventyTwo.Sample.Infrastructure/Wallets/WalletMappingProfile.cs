using Mapster;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

public sealed class WalletMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<WalletRecord, Wallet>()
            .ConstructUsing(x => new Wallet(x.Id, x.CustomerId, x.Currency, new Money(x.BalanceAmount)))
            .Map(x => x.WalletType, x => x.Currency)
            .Map(x => x.Balance, x => new Money(x.BalanceAmount));
        config
            .NewConfig<Wallet, WalletRecord>()
            .Map(x => x.Currency, x => x.WalletType)
            .Map(x => x.BalanceAmount, x => x.Balance.Value);
    }
}
