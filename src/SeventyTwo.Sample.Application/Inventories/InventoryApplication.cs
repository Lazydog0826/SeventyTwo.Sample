using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.Domain.Inventories;
using Yitter.IdGenerator;

namespace SeventyTwo.Sample.Application.Inventories;

[AutofacDependency(typeof(IInventoryApplication))]
public sealed class InventoryApplication(
    IInventoryRepository inventoryRepository
) : IInventoryApplication
{
    public async Task<ChangeInventoryResult> ChangeAsync(
        ChangeInventoryInput input,
        CancellationToken cancellationToken
    )
    {
        if (input.InventoryId <= 0)
        {
            throw new InventoryDomainException("库存 ID 必须大于 0");
        }

        if (string.IsNullOrWhiteSpace(input.RequestNo))
        {
            throw new InventoryDomainException("业务请求号不能为空");
        }

        if (input.RequestNo.Length > 64)
        {
            throw new InventoryDomainException("业务请求号长度不能超过 64 个字符");
        }

        if (!Enum.IsDefined(input.ChangeType))
        {
            throw new InventoryDomainException("库存变更类型无效");
        }

        if (input.Quantity <= 0)
        {
            throw new InventoryDomainException("库存变更数量必须大于 0");
        }

        var draft = new InventoryChangeDraft(
            YitIdHelper.NextId(),
            input.RequestNo,
            input.InventoryId,
            input.ChangeType,
            input.Quantity,
            DateTimeOffset.UtcNow
        );
        var change = await inventoryRepository.ChangeAsync(draft, cancellationToken);

        return new ChangeInventoryResult(
            change.Id,
            change.RequestNo,
            change.InventoryId,
            change.ChangeType,
            change.Quantity,
            change.BeforeQuantity,
            change.AfterQuantity
        );
    }
}
