using AutoMapper;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

public sealed class WalletMappingProfile : Profile
{
    public WalletMappingProfile()
    {
        CreateMap<WalletRecord, Wallet>()
            .ConstructUsing(x => new Wallet(x.Id, x.CustomerId, x.Currency, new Money(x.BalanceAmount)))
            .ForMember(x => x.WalletType, options => options.MapFrom(x => x.Currency))
            .ForMember(x => x.Balance, options => options.MapFrom(x => new Money(x.BalanceAmount)));
        CreateMap<Wallet, WalletRecord>()
            .ForMember(x => x.Currency, options => options.MapFrom(x => x.WalletType))
            .ForMember(x => x.BalanceAmount, options => options.MapFrom(x => x.Balance.Value));
    }
}
