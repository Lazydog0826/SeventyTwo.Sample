using SeventyTwo.InfraKit.Core.DomainAggregateRoot;

namespace SeventyTwo.Sample.Domain.Inventories;

public sealed class Inventory : AggregateRoot
{
    private Inventory() { }

    public Inventory(long id, long productId, long warehouseId, long locationId, int quantity)
    {
        if (id <= 0)
        {
            throw new InventoryDomainException("库存 ID 必须大于 0");
        }

        if (productId <= 0)
        {
            throw new InventoryDomainException("商品 ID 必须大于 0");
        }

        if (warehouseId <= 0)
        {
            throw new InventoryDomainException("仓库 ID 必须大于 0");
        }

        if (locationId <= 0)
        {
            throw new InventoryDomainException("货位 ID 必须大于 0");
        }

        if (quantity < 0)
        {
            throw new InventoryDomainException("库存数量不能小于 0");
        }

        Id = id;
        ProductId = productId;
        WarehouseId = warehouseId;
        LocationId = locationId;
        Quantity = quantity;
    }

    public long ProductId { get; private set; }

    public long WarehouseId { get; private set; }

    public long LocationId { get; private set; }

    public int Quantity { get; private set; }
}
