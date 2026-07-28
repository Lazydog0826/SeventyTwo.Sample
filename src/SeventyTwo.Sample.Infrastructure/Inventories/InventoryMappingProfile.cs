using AutoMapper;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Infrastructure.Inventories;

public sealed class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        CreateMap<InventoryRecord, Inventory>()
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
        CreateMap<Inventory, InventoryRecord>()
            .ForMember(x => x.Key, options => options.MapFrom(x => $"{x.WarehouseId}:{x.LocationId}:{x.ProductId}"));
    }
}
