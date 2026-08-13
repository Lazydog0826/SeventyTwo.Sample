using Mapster;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Application.Inventories;

/// <summary>
/// 配置库存应用输入到领域变更草稿的映射。
/// </summary>
public sealed class InventoryDraftMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InventoryIncreaseInput, InventoryIncreaseDraft>();
        config.NewConfig<InventoryDecreaseInput, InventoryDecreaseDraft>();
        config
            .NewConfig<ChangeInventoryInput, InventoryChangeDraft>()
            .ConstructUsing(source => new InventoryChangeDraft(
                source.RequestNo,
                source.Increases.Adapt<List<InventoryIncreaseDraft>>(),
                source.Decreases.Adapt<List<InventoryDecreaseDraft>>()
            ));
    }
}
