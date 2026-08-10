using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Domain.Permissions;
using StackExchange.Redis;

namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 权限缓存键。
/// </summary>
public static class PermissionCacheKeys
{
    public static string GetAllPermissionsLockKey(CacheConfiguration configuration)
    {
        return configuration.Lock("permissions", "lock");
    }

    public static string GetAllPermissionsVersionKey(CacheConfiguration configuration)
    {
        return configuration.Data("permissions", "version");
    }

    public static string GetAllPermissionsMetaKey(CacheConfiguration configuration, string version)
    {
        return configuration.Data("permissions", "meta:" + version);
    }

    public static string GetAllPermissionsBucketKey(CacheConfiguration configuration, string bucket)
    {
        return configuration.Data("permissions", "bucket:" + bucket);
    }
}

/// <summary>
/// 管理所有权限的 Redis 分片缓存，并通过分布式锁协调多个服务实例的缓存加载与失效。
/// </summary>
/// <param name="redisCacheService">Redis 缓存及分布式锁服务。</param>
/// <param name="cacheConfiguration">缓存键配置。</param>
[AutofacDependency(ServiceLifetime = ServiceLifetime.Singleton)]
public sealed class PermissionCacheService(
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
)
{
    private const int BucketSize = 10;
    private static readonly TimeSpan VersionCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MetaCacheExpiration = TimeSpan.FromMinutes(31);
    private static readonly TimeSpan BucketCacheExpiration = TimeSpan.FromMinutes(32);
    private static readonly TimeSpan LoadLockAcquireTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan InvalidationLockAcquireTimeout = TimeSpan.FromMinutes(2.5);
    private static readonly TimeSpan LockRenewalInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockLeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockExecutionTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 获取全部权限；缓存不存在时，在分布式锁内调用加载器并写入分片缓存。
    /// </summary>
    /// <param name="loader">缓存未命中时使用的权限加载器。</param>
    /// <param name="cancellationToken">用于取消缓存读取、权限加载和锁等待的令牌。</param>
    /// <returns>缓存中或加载器返回的全部权限。</returns>
    public async Task<IReadOnlyList<Permission>> GetOrLoadAsync(
        Func<CancellationToken, Task<IReadOnlyList<Permission>>> loader,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redisCacheService.GetDatabase();
        var configuration = cacheConfiguration.Value;
        var cachedPermissions = await TryGetCachedAsync(database, configuration);
        cancellationToken.ThrowIfCancellationRequested();
        if (cachedPermissions is not null)
        {
            return cachedPermissions;
        }

        IReadOnlyList<Permission>? permissions = null;
        var lockKey = PermissionCacheKeys.GetAllPermissionsLockKey(configuration);
        await redisCacheService.LockAsync(
            lockKey,
            async lockCancellationToken =>
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lockCancellationToken
                );
                var operationCancellationToken = operationCts.Token;
                operationCancellationToken.ThrowIfCancellationRequested();

                // 等待锁期间可能已有其他实例完成缓存加载。
                permissions = await TryGetCachedAsync(database, configuration);
                operationCancellationToken.ThrowIfCancellationRequested();
                if (permissions is not null)
                {
                    return;
                }

                permissions = await loader(operationCancellationToken);
                operationCancellationToken.ThrowIfCancellationRequested();
                await SetCacheAsync(database, configuration, permissions, operationCancellationToken);
            },
            acquisitionTimeout: LoadLockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockLeaseDuration,
            executionTimeout: LockExecutionTimeout,
            cancellationToken: cancellationToken
        );

        return permissions ?? throw new InvalidOperationException("权限缓存加载失败");
    }

    /// <summary>
    /// 使当前权限缓存版本失效；关联的元数据和分片由过期时间自动清理。
    /// </summary>
    /// <param name="cancellationToken">用于取消锁等待和失效操作的令牌。</param>
    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        var configuration = cacheConfiguration.Value;
        var lockKey = PermissionCacheKeys.GetAllPermissionsLockKey(configuration);
        var database = redisCacheService.GetDatabase();
        await redisCacheService.LockAsync(
            lockKey,
            async lockCancellationToken =>
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lockCancellationToken
                );
                var operationCancellationToken = operationCts.Token;
                operationCancellationToken.ThrowIfCancellationRequested();

                await database.KeyDeleteAsync(PermissionCacheKeys.GetAllPermissionsVersionKey(configuration));

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
    /// 根据当前版本读取权限分片缓存。
    /// </summary>
    /// <param name="database">Redis 数据库。</param>
    /// <param name="configuration">缓存键配置。</param>
    /// <returns>缓存有效时返回权限集合；版本或元数据不存在时返回 <see langword="null" />。</returns>
    private static async Task<IReadOnlyList<Permission>?> TryGetCachedAsync(
        IDatabase database,
        CacheConfiguration configuration
    )
    {
        var version = await database.StringGetAsync(PermissionCacheKeys.GetAllPermissionsVersionKey(configuration));
        if (!version.HasValue)
        {
            return null;
        }

        var metaValue = await database.StringGetAsync(
            PermissionCacheKeys.GetAllPermissionsMetaKey(configuration, version.ToString())
        );
        if (!metaValue.HasValue)
        {
            return null;
        }

        try
        {
            var meta = JsonSerializer.Deserialize<PermissionCacheMeta>(metaValue.ToString());
            if (meta is null)
            {
                return null;
            }

            var bucketTasks = meta.BucketKeys.Select(key => database.StringGetAsync(key)).ToArray();
            var buckets = await Task.WhenAll(bucketTasks);
            if (buckets.Any(bucket => !bucket.HasValue))
            {
                return null;
            }

            var permissions = new List<Permission>();
            foreach (var bucket in buckets)
            {
                var cacheItems = JsonSerializer.Deserialize<PermissionCacheItem[]>(bucket.ToString());
                if (cacheItems is null)
                {
                    return null;
                }

                permissions.AddRange(cacheItems.Select(item => item.ToPermission()));
            }

            return permissions;
        }
        catch (Exception exception) when (exception is JsonException or PermissionDomainException)
        {
            // 任一缓存项无效时重新加载全部权限，避免返回不完整数据。
            return null;
        }
    }

    /// <summary>
    /// 将权限写入分片和元数据，并在所有数据写入完成后发布新版本。
    /// </summary>
    /// <param name="database">Redis 数据库。</param>
    /// <param name="configuration">缓存键配置。</param>
    /// <param name="permissions">需要缓存的全部权限。</param>
    /// <param name="cancellationToken">用于停止后续缓存写入和版本发布的令牌。</param>
    private static async Task SetCacheAsync(
        IDatabase database,
        CacheConfiguration configuration,
        IReadOnlyList<Permission> permissions,
        CancellationToken cancellationToken
    )
    {
        var version = Guid.CreateVersion7().ToString();
        var buckets = permissions
            .Chunk(BucketSize)
            .Select(bucket =>
            {
                var key = PermissionCacheKeys.GetAllPermissionsBucketKey(
                    configuration,
                    Guid.CreateVersion7().ToString()
                );
                var values = bucket.Select(PermissionCacheItem.FromPermission).ToArray();
                return (Key: (RedisKey)key, Value: (RedisValue)JsonSerializer.Serialize(values));
            })
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();
        await Task.WhenAll(
            buckets.Select(bucket => database.StringSetAsync(bucket.Key, bucket.Value, BucketCacheExpiration))
        );
        cancellationToken.ThrowIfCancellationRequested();

        var metaKey = PermissionCacheKeys.GetAllPermissionsMetaKey(configuration, version);
        var meta = new PermissionCacheMeta([.. buckets.Select(bucket => bucket.Key.ToString())]);
        await database.StringSetAsync(metaKey, JsonSerializer.Serialize(meta), MetaCacheExpiration);
        cancellationToken.ThrowIfCancellationRequested();

        // 最后发布版本，确保读取方只会看到已写完的缓存。
        await database.StringSetAsync(
            PermissionCacheKeys.GetAllPermissionsVersionKey(configuration),
            version,
            VersionCacheExpiration
        );
        cancellationToken.ThrowIfCancellationRequested();
    }

    private sealed record PermissionCacheMeta(string[] BucketKeys);

    /// <summary>
    /// 权限缓存传输对象，用于隔离领域模型与 JSON 序列化协议。
    /// </summary>
    private sealed record PermissionCacheItem(
        Guid Id,
        string Code,
        string Title,
        PermissionType Type,
        int SortOrder,
        string Icon,
        string VueComponentPath,
        string RoutePath,
        string RouteName,
        Guid? ParentId,
        PermissionMetaData MetaData,
        bool Enable,
        Guid? DeleteBy,
        DateTimeOffset? DeleteAt,
        Guid CreatedBy,
        DateTimeOffset CreatedAt,
        Guid? UpdatedBy,
        DateTimeOffset? UpdatedAt,
        Guid OrgId,
        Guid Version
    )
    {
        public static PermissionCacheItem FromPermission(Permission permission)
        {
            return new PermissionCacheItem(
                permission.Id,
                permission.Code,
                permission.Title,
                permission.Type,
                permission.SortOrder,
                permission.Icon,
                permission.VueComponentPath,
                permission.RoutePath,
                permission.RouteName,
                permission.ParentId,
                permission.MetaData,
                permission.Enable,
                permission.DeleteBy,
                permission.DeleteAt,
                permission.CreatedBy,
                permission.CreatedAt,
                permission.UpdatedBy,
                permission.UpdatedAt,
                permission.OrgId,
                permission.Version
            );
        }

        public Permission ToPermission()
        {
            return new Permission(
                Id,
                Code,
                Title,
                Type,
                SortOrder,
                Icon,
                VueComponentPath,
                RoutePath,
                RouteName,
                ParentId,
                MetaData
            )
            {
                Enable = Enable,
                DeleteBy = DeleteBy,
                DeleteAt = DeleteAt,
                CreatedBy = CreatedBy,
                CreatedAt = CreatedAt,
                UpdatedBy = UpdatedBy,
                UpdatedAt = UpdatedAt,
                OrgId = OrgId,
                Version = Version,
            };
        }
    }
}
