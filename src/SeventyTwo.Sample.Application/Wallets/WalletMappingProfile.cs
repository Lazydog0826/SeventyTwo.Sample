using Mapster;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets;

/// <summary>
/// 配置钱包应用模型与领域变更模型之间的映射。
/// </summary>
public sealed class WalletCommandMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<BalanceChangeDetailInput, BalanceChangeDetailCommand>()
            .Map(destination => destination.WalletType, source => source.Currency)
            .Map(destination => destination.Amount, source => new Money(source.Amount));
        config
            .NewConfig<BalanceChangeInput, BalanceChangeCommand>()
            .ConstructUsing(source => new BalanceChangeCommand(
                source.CustomerId,
                source.RequestNo,
                source.Details.Adapt<List<BalanceChangeDetailCommand>>()
            ));
        config.NewConfig<BalanceChangeDetailCommand, WalletBalanceChangeRequest>();
    }
}
