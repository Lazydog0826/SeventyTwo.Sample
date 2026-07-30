using Mapster;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Infrastructure.Inventories;

public sealed class InventoryMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<InventoryRecord, Inventory>()
            .ConstructUsing(x => new Inventory(
                x.Id,
                x.ProductId,
                x.WarehouseId,
                x.LocationId,
                x.InboundBatchNo,
                x.InboundAt,
                x.InitialQuantity,
                x.Quantity
            ));
        config
            .NewConfig<Inventory, InventoryRecord>()
            .Map(x => x.Key, x => $"{x.WarehouseId}:{x.LocationId}:{x.ProductId}");
    }
}
