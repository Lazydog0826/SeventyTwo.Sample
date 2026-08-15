using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Domain.Organizations;

namespace SeventyTwo.Sample.Application.Organizations;

/// <summary>
/// 按机构 ID 缓存机构层级路径，并通过分布式锁避免缓存击穿。
/// </summary>
[AutofacDependency]
public sealed class OrganizationsCacheService(
    IOrganizationRepository organizationRepository,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
)
{
    private const string EmptyCacheValue = "null";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LoadLockAcquireTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan InvalidationLockAcquireTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LockRenewalInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockLeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockExecutionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 获取指定机构的层级路径；机构不存在时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="cancellationToken">用于取消锁等待和缓存加载的令牌。</param>
    /// <returns>机构层级路径；机构不存在时返回 <see langword="null"/>。</returns>
    public async Task<string?> FindPathAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cacheKey = GetCacheKey(id);
        var database = redisCacheService.GetDatabase();
        var cachedResult = await GetCacheAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (cachedResult.Found)
        {
            return cachedResult.Path;
        }

        string? path = null;

        await redisCacheService.LockAsync(
            GetLockKey(id),
            async lockCancellationToken =>
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lockCancellationToken
                );
                var operationCancellationToken = operationCts.Token;
                operationCancellationToken.ThrowIfCancellationRequested();

                // 等待锁期间可能已有其他实例完成缓存加载。
                cachedResult = await GetCacheAsync();
                operationCancellationToken.ThrowIfCancellationRequested();
                if (cachedResult.Found)
                {
                    path = cachedResult.Path;
                    return;
                }

                var organization = await organizationRepository.FindAsync(id, operationCancellationToken);
                if (organization is null)
                {
                    await database.StringSetAsync(cacheKey, EmptyCacheValue, CacheDuration);
                    operationCancellationToken.ThrowIfCancellationRequested();
                    return;
                }

                path = organization.Path;
                await database.StringSetAsync(cacheKey, path, CacheDuration);
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: LoadLockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockLeaseDuration,
            executionTimeout: LockExecutionTimeout,
            cancellationToken: cancellationToken
        );

        return path;

        // 缓存值只存机构 Path 字符串；机构 ID 拼接的路径不会与空值哨兵冲突。
        async Task<(bool Found, string? Path)> GetCacheAsync()
        {
            var cachedValue = await database.StringGetAsync(cacheKey);
            if (!cachedValue.HasValue)
            {
                return (false, null);
            }

            var value = cachedValue.ToString();
            return value == EmptyCacheValue ? (true, null) : (true, value);
        }
    }

    /// <summary>
    /// 删除指定机构的路径缓存。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="cancellationToken">用于取消锁等待和缓存删除的令牌。</param>
    public async Task DeleteCacheAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redisCacheService.GetDatabase();
        await redisCacheService.LockAsync(
            GetLockKey(id),
            async lockCancellationToken =>
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lockCancellationToken
                );
                var operationCancellationToken = operationCts.Token;
                operationCancellationToken.ThrowIfCancellationRequested();
                await database.KeyDeleteAsync(GetCacheKey(id));
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: InvalidationLockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockLeaseDuration,
            executionTimeout: LockExecutionTimeout,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// 获取指定机构的路径缓存键。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <returns>机构路径缓存键。</returns>
    private string GetCacheKey(Guid id)
    {
        return cacheConfiguration.Value.Data("organizations", $"path:{id}");
    }

    /// <summary>
    /// 获取指定机构的路径缓存锁键。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <returns>机构路径缓存锁键。</returns>
    private string GetLockKey(Guid id)
    {
        return cacheConfiguration.Value.Lock("organizations", $"path:{id}");
    }
}
