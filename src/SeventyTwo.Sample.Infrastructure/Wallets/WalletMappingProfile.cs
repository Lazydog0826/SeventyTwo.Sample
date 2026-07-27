using AutoMapper;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

public sealed class WalletMappingProfile : Profile
{
    public WalletMappingProfile()
    {
        CreateMap<WalletRecord, Wallet>()
            .ConstructUsing(x => new Wallet(x.Id, x.CustomerId, x.Currency, x.BalanceAmount));
        CreateMap<Wallet, WalletRecord>();
        CreateMap<WalletChangeRecordDraft, WalletChangeRecord>();
    }
}
