using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Wallets;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf

namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

[AutofacDependency(typeof(IBalanceChangeService))]
public sealed class BalanceChangeService(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration,
    IMemoryCache memoryCache
) : IBalanceChangeService
{
    private readonly WalletBalanceChangeService _walletBalanceChangeService = new();

    public async Task BalanceChangeAsync(BalanceChangeCommand command, CancellationToken cancellationToken)
    {
        var walletTypes = command.Details.Select(x => x.WalletType).Distinct().ToList();
        var requests = command
            .Details.Select(x => new WalletBalanceChangeRequest(x.WalletType, x.ChangeType, x.Amount))
            .ToList();

        var allKeys = new List<string> { command.CustomerId.ToString() };
        var keys = await CheckWalletKeysExistAsync(allKeys);

        if (keys.Any())
        {
            await walletRepository.EnsureChangeLocksAsync(keys, cancellationToken);
        }

        await SetWalletKeysCacheAsync(keys);

        await unitOfWork.ExecuteAsync(
            async () =>
            {
                var registered = await walletRepository.TryRegisterBalanceChangeAsync(
                    command.RequestNo,
                    cancellationToken
                );
                if (!registered)
                {
                    return;
                }

                var walletList = await walletRepository.GetForBalanceChangeAsync(
                    command.CustomerId,
                    walletTypes,
                    allKeys,
                    cancellationToken
                );

                var batch = _walletBalanceChangeService.Change(
                    command.CustomerId,
                    walletList,
                    requests,
                    Guid.CreateVersion7
                );
                var createdAt = DateTimeExtension.Now();
                foreach (var wallet in batch.NewWallets)
                {
                    wallet.CreatedBy = SystemIds.System;
                    wallet.CreatedAt = createdAt;
                }

                await walletRepository.SaveBalanceChangeAsync(
                    command.RequestNo,
                    batch.NewWallets,
                    batch.ChangedWallets,
                    batch.Changes,
                    cancellationToken
                );
            },
            cancellationToken
        );
    }

    private string GetCacheKey(string key)
    {
        return cacheConfiguration.Value.Data("wallets", $"customer-key:{key}");
    }

    private async Task<List<string>> CheckWalletKeysExistAsync(List<string> keys)
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

    private async Task SetWalletKeysCacheAsync(List<string> keys)
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
        var disabledUntil = memoryCache.Get<long>(cacheConfiguration.Value.Data("common", "redis-disabled-until-ticks"));
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
