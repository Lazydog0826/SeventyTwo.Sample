// ReSharper disable MemberCanBeMadeStatic.Global
namespace SeventyTwo.Sample.Domain.Inventories;

public enum InventoryChangeType : short
{
    Increase = 1,
    Decrease = 2,
}

public readonly record struct InventoryDimension(Guid ProductId, Guid WarehouseId, Guid LocationId);

public sealed record InventoryChange(
    Guid InventoryId,
    InventoryChangeType ChangeType,
    int Quantity,
    int BeforeQuantity,
    int AfterQuantity,
    DateTimeOffset ChangedAt
);

public sealed record InventoryChangeBatch(
    IReadOnlyCollection<Inventory> NewInventories,
    IReadOnlyCollection<Inventory> ChangedInventories,
    IReadOnlyCollection<InventoryChange> Changes
);

public sealed class InventoryChangeDraft
{
    public InventoryChangeDraft(
        Guid requestNo,
        List<InventoryIncreaseDraft> increases,
        List<InventoryDecreaseDraft> decreases
    )
    {
        if (requestNo == Guid.Empty)
        {
            throw new InventoryDomainException("业务请求号不能为空");
        }

        RequestNo = requestNo;
        Increases = increases.ToList().AsReadOnly();
        Decreases = decreases.ToList().AsReadOnly();
    }

    public Guid RequestNo { get; }

    public IReadOnlyList<InventoryIncreaseDraft> Increases { get; }

    public IReadOnlyList<InventoryDecreaseDraft> Decreases { get; }
}

public record InventoryDraft
{
    protected InventoryDraft(Guid productId, Guid warehouseId, Guid locationId, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new InventoryDomainException("商品 ID 不能为空");
        }

        if (warehouseId == Guid.Empty)
        {
            throw new InventoryDomainException("仓库 ID 不能为空");
        }

        if (locationId == Guid.Empty)
        {
            throw new InventoryDomainException("货位 ID 不能为空");
        }

        if (quantity <= 0)
        {
            throw new InventoryDomainException("库存变更数量必须大于 0");
        }

        ProductId = productId;
        WarehouseId = warehouseId;
        LocationId = locationId;
        Quantity = quantity;
    }

    public Guid ProductId { get; }

    public Guid WarehouseId { get; }

    public Guid LocationId { get; }

    public int Quantity { get; }
}

public sealed record InventoryIncreaseDraft : InventoryDraft
{
    public InventoryIncreaseDraft(
        Guid productId,
        Guid warehouseId,
        Guid locationId,
        int quantity,
        string inboundBatchNo,
        DateTimeOffset changedAt
    )
        : base(productId, warehouseId, locationId, quantity)
    {
        if (string.IsNullOrWhiteSpace(inboundBatchNo))
        {
            throw new InventoryDomainException("入库批次号不能为空");
        }

        if (inboundBatchNo.Length > 64)
        {
            throw new InventoryDomainException("入库批次号长度不能超过 64 个字符");
        }

        if (changedAt == default)
        {
            throw new InventoryDomainException("入库时间不能为空");
        }

        InboundBatchNo = inboundBatchNo;
        ChangedAt = changedAt;
    }

    public string InboundBatchNo { get; }

    public DateTimeOffset ChangedAt { get; }
}

public sealed record InventoryDecreaseDraft : InventoryDraft
{
    public InventoryDecreaseDraft(Guid productId, Guid warehouseId, Guid locationId, int quantity)
        : base(productId, warehouseId, locationId, quantity) { }
}

public sealed class InventoryChangeService
{
    public InventoryChangeBatch Change(
        IReadOnlyCollection<Inventory> inventories,
        InventoryChangeDraft draft,
        Func<Guid> nextInventoryId,
        DateTimeOffset changedAt
    )
    {
        if (changedAt == default)
        {
            throw new InventoryDomainException("库存变更时间不能为空");
        }

        var newInventories = new List<Inventory>();
        var changedInventories = new List<Inventory>();
        var changes = new List<InventoryChange>();
        var inventoryList = inventories.ToList();
        var newInventoryIds = new HashSet<Guid>();
        var changedInventoryIds = new HashSet<Guid>();

        foreach (var increase in draft.Increases)
        {
            var inventory = new Inventory(
                nextInventoryId(),
                increase.ProductId,
                increase.WarehouseId,
                increase.LocationId,
                increase.InboundBatchNo,
                increase.ChangedAt,
                increase.Quantity
            );
            newInventories.Add(inventory);
            inventoryList.Add(inventory);
            newInventoryIds.Add(inventory.Id);
            changes.Add(
                new InventoryChange(
                    inventory.Id,
                    InventoryChangeType.Increase,
                    increase.Quantity,
                    0,
                    increase.Quantity,
                    increase.ChangedAt
                )
            );
        }

        var decreases = draft
            .Decreases.GroupBy(x => new InventoryDimension(x.ProductId, x.WarehouseId, x.LocationId))
            .Select(x => new InventoryDecreaseDraft(
                x.Key.ProductId,
                x.Key.WarehouseId,
                x.Key.LocationId,
                x.Sum(item => item.Quantity)
            ));

        foreach (var decrease in decreases)
        {
            var remainingQuantity = decrease.Quantity;
            var matchingInventories = inventoryList
                .Where(x =>
                    x.ProductId == decrease.ProductId
                    && x.WarehouseId == decrease.WarehouseId
                    && x.LocationId == decrease.LocationId
                    && x.Quantity > 0
                )
                .OrderByDescending(x => x.InboundAt)
                .ToList();

            foreach (var inventory in matchingInventories)
            {
                var quantity = Math.Min(inventory.Quantity, remainingQuantity);
                var quantityChange = inventory.Decrease(quantity);
                remainingQuantity -= quantity;

                changes.Add(
                    new InventoryChange(
                        inventory.Id,
                        InventoryChangeType.Decrease,
                        quantity,
                        quantityChange.BeforeQuantity,
                        quantityChange.AfterQuantity,
                        changedAt
                    )
                );

                if (!newInventoryIds.Contains(inventory.Id) && changedInventoryIds.Add(inventory.Id))
                {
                    changedInventories.Add(inventory);
                }

                if (remainingQuantity == 0)
                {
                    break;
                }
            }

            if (remainingQuantity > 0)
            {
                throw new InventoryDomainException("库存不足");
            }
        }

        return new InventoryChangeBatch(newInventories, changedInventories, changes);
    }
}
