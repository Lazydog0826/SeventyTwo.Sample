using Mapster;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;
using SeventyTwo.Sample.WebApi.Contracts.Wallets;

namespace SeventyTwo.Sample.WebApi.Mappings;

/// <summary>
/// 配置钱包接口请求到应用层输入模型的映射。
/// </summary>
public sealed class WalletRequestMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BalanceChangeDetailRequest, BalanceChangeDetailInput>();
        config.NewConfig<BalanceChangeRequest, BalanceChangeInput>();
    }
}
