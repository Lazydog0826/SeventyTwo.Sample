using Mapster;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.WebApi.Contracts.Inventories;

namespace SeventyTwo.Sample.WebApi.Mappings;

/// <summary>
/// 配置库存接口请求到应用层输入模型的映射。
/// </summary>
public sealed class InventoryRequestMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InventoryIncreaseRequest, InventoryIncreaseInput>();
        config.NewConfig<InventoryDecreaseRequest, InventoryDecreaseInput>();
        config.NewConfig<ChangeInventoryRequest, ChangeInventoryInput>();
    }
}
