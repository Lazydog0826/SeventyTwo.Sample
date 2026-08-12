using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Inventories;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf

namespace SeventyTwo.Sample.Application.Inventories.ChangeInventory;

[AutofacDependency(typeof(IChangeInventoryService))]
public sealed class ChangeInventoryService(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration,
    IMemoryCache memoryCache
) : IChangeInventoryService
{
    private readonly InventoryChangeService _inventoryChangeService = new();

    public async Task ChangeAsync(InventoryChangeDraft draft, CancellationToken cancellationToken)
    {
        if (draft.Increases.Count == 0 && draft.Decreases.Count == 0)
        {
            return;
        }

        var allKeys = draft
            .Decreases.Select(x => new InventoryDimension(x.ProductId, x.WarehouseId, x.LocationId))
            .Select(GetKey)
            .Distinct()
            .ToList();

        var keys = await CheckInventoriesKeyExistAsync(allKeys);

        // 规范：Redis 只用于减少锁记录的幂等插入；维度锁初始化必须在无外层数据库事务时提交，且事务内基于数据库行锁的锁完整性校验不得移除。
        // 扣减才需要确保维度锁，纯新增不需要
        if (keys.Any())
        {
            await inventoryRepository.EnsureChangeLocksAsync(keys, cancellationToken);
        }

        await SetInventoriesKeyCacheAsync(keys);

        await unitOfWork.ExecuteAsync(
            async () =>
            {
                var registered = await inventoryRepository.TryRegisterChangeAsync(draft.RequestNo, cancellationToken);
                if (!registered)
                {
                    return;
                }

                var inventories = await inventoryRepository.GetForChangeAsync(allKeys, cancellationToken);
                var changedAt = DateTimeExtension.Now();
                var batch = _inventoryChangeService.Change(inventories, draft, Guid.CreateVersion7, changedAt);

                foreach (var inventory in batch.NewInventories)
                {
                    inventory.CreatedBy = SystemIds.System;
                    inventory.CreatedAt = changedAt;
                }

                await inventoryRepository.SaveChangeAsync(
                    draft.RequestNo,
                    batch.NewInventories,
                    batch.ChangedInventories,
                    batch.Changes,
                    cancellationToken
                );
            },
            cancellationToken
        );
    }

    private static string GetKey(InventoryDimension dimension)
    {
        return $"{dimension.WarehouseId}:{dimension.LocationId}:{dimension.ProductId}";
    }

    private string GetCacheKey(string key)
    {
        return cacheConfiguration.Value.Data("inventories", $"dim-key:{key}");
    }

    private async Task<List<string>> CheckInventoriesKeyExistAsync(List<string> keys)
    {
        var noExistList = new List<string>();

        foreach (var key in keys)
        {
            if (CanUseRedis())
            {
                try
                {
                    var isExist = await redisCacheService.GetDatabase().KeyExistsAsync(GetCacheKey(key));
                    if (!isExist)
                    {
                        noExistList.Add(key);
                    }
                }
                catch
                {
                    noExistList.Add(key);
                    // redis 错误后 60 秒内不再使用 redis
                    SetRedisCircuitBreaker();
                }
            }
            else
            {
                noExistList.Add(key);
            }
        }

        return noExistList;
    }

    private async Task SetInventoriesKeyCacheAsync(List<string> keys)
    {
        var timeSpan = TimeSpan.FromDays(1);
        foreach (var key in keys)
        {
            if (CanUseRedis())
            {
                try
                {
                    await redisCacheService.GetDatabase().StringSetAsync(GetCacheKey(key), 0, timeSpan);
                }
                catch
                {
                    // redis 错误后 60 秒内不再使用 redis
                    SetRedisCircuitBreaker();
                }
            }
        }
    }

    private bool CanUseRedis()
    {
        var disabledUntil = memoryCache.Get<long>(
            cacheConfiguration.Value.Data("common", "redis-disabled-until-ticks")
        );
        return DateTimeOffset.UtcNow.UtcTicks >= disabledUntil;
    }

    private void SetRedisCircuitBreaker()
    {
        memoryCache.Set(
            cacheConfiguration.Value.Data("common", "redis-disabled-until-ticks"),
            DateTimeOffset.UtcNow.UtcTicks + TimeSpan.FromMinutes(1).Ticks
        );
    }
}
