using System.Text.Json;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 基于 Redis 缓存用户有效权限编码，并通过全局版本切换使普通用户缓存整体失效。
/// </summary>
/// <param name="permissionRepository">权限仓储。</param>
/// <param name="userInfoCacheService">用户信息缓存服务。</param>
/// <param name="redisCacheService">Redis 缓存及分布式锁服务。</param>
/// <param name="cacheConfiguration">缓存键配置。</param>
[AutofacDependency(typeof(IUserPermissionCacheService))]
public sealed class UserPermissionCacheService(
    IPermissionRepository permissionRepository,
    UserInfoCacheService userInfoCacheService,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : IUserPermissionCacheService
{
    /// <summary>
    /// 单个用户权限编码缓存有效期。
    /// </summary>
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 缓存锁获取超时时间。
    /// </summary>
    private static readonly TimeSpan LockAcquireTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 缓存锁租约及执行超时时间。
    /// </summary>
    private static readonly TimeSpan LockExpiration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 缓存锁续租间隔。
    /// </summary>
    private static readonly TimeSpan LockRenewalInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 获取超级管理员共享权限缓存键。
    /// </summary>
    /// <returns>超级管理员共享权限缓存键。</returns>
    private string GetSuperAdminCacheKey()
    {
        return cacheConfiguration.Value.Data("permissions", "user-codes:super-admin");
    }

    /// <summary>
    /// 获取超级管理员共享权限缓存锁键。
    /// </summary>
    /// <returns>超级管理员共享权限缓存锁键。</returns>
    private string GetSuperAdminLockKey()
    {
        return cacheConfiguration.Value.Lock("permissions", "user-codes:super-admin");
    }

    /// <summary>
    /// 获取普通用户权限缓存全局版本键。
    /// </summary>
    /// <returns>普通用户权限缓存全局版本键。</returns>
    private string GetVersionKey()
    {
        return cacheConfiguration.Value.Data("permissions", "user-codes:version");
    }

    /// <summary>
    /// 获取普通用户权限缓存全局版本锁键。
    /// </summary>
    /// <returns>普通用户权限缓存全局版本锁键。</returns>
    private string GetVersionLockKey()
    {
        return cacheConfiguration.Value.Lock("permissions", "user-codes:version");
    }

    /// <summary>
    /// 获取指定版本和用户的权限编码缓存键。
    /// </summary>
    /// <param name="version">普通用户权限缓存全局版本。</param>
    /// <param name="userId">用户 ID。</param>
    /// <returns>指定版本和用户的权限编码缓存键。</returns>
    private string GetCacheKey(string version, Guid userId)
    {
        return cacheConfiguration.Value.Data("permissions", $"user-codes:{version}:{userId}");
    }

    /// <summary>
    /// 获取指定版本和用户的权限编码缓存锁键。
    /// </summary>
    /// <param name="version">普通用户权限缓存全局版本。</param>
    /// <param name="userId">用户 ID。</param>
    /// <returns>指定版本和用户的权限编码缓存锁键。</returns>
    private string GetLockKey(string version, Guid userId)
    {
        return cacheConfiguration.Value.Lock("permissions", $"user-codes:{version}:{userId}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var database = redisCacheService.GetDatabase();
        var isSuperAdmin = await userInfoCacheService.IsSuperAdminAsync(userId, cancellationToken);
        var version = isSuperAdmin ? null : await GetOrCreateVersionAsync(database, cancellationToken);
        var cacheKey = isSuperAdmin ? GetSuperAdminCacheKey() : GetCacheKey(version!, userId);
        var cachedValue = await GetCacheAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        var lockKey = isSuperAdmin ? GetSuperAdminLockKey() : GetLockKey(version!, userId);
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
                cachedValue = await GetCacheAsync();
                operationCancellationToken.ThrowIfCancellationRequested();
                if (cachedValue is not null)
                {
                    return;
                }

                IReadOnlyList<string> permissionCodes;
                if (isSuperAdmin)
                {
                    var permissions = await permissionRepository.GetAllAsync(operationCancellationToken);
                    permissionCodes = [.. permissions.Select(permission => permission.Code)];
                }
                else
                {
                    permissionCodes = await permissionRepository.GetCodesByUserIdAsync(
                        userId,
                        operationCancellationToken
                    );
                }

                await database.StringSetAsync(cacheKey, JsonSerializer.Serialize(permissionCodes), CacheExpiration);

                operationCancellationToken.ThrowIfCancellationRequested();
                cachedValue = permissionCodes;
            },
            acquisitionTimeout: LockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockExpiration,
            executionTimeout: LockExpiration,
            cancellationToken: cancellationToken
        );

        return cachedValue ?? [];

        async Task<IReadOnlyList<string>?> GetCacheAsync()
        {
            var serializedValue = await database.StringGetAsync(cacheKey);
            if (!serializedValue.HasValue)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<string[]>(serializedValue.ToString()) ?? [];
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var database = redisCacheService.GetDatabase();
        var version = await GetOrCreateVersionAsync(database, cancellationToken);
        await DeleteCacheAsync(GetCacheKey(version, userId), GetLockKey(version, userId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteSuperAdminAsync(CancellationToken cancellationToken)
    {
        var database = redisCacheService.GetDatabase();
        await redisCacheService.LockAsync(
            GetVersionLockKey(),
            async lockCancellationToken =>
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lockCancellationToken
                );
                var operationCancellationToken = operationCts.Token;
                operationCancellationToken.ThrowIfCancellationRequested();
                await database.StringSetAsync(GetVersionKey(), Guid.CreateVersion7().ToString());
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: LockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockExpiration,
            executionTimeout: LockExpiration,
            cancellationToken: cancellationToken
        );
        await DeleteCacheAsync(GetSuperAdminCacheKey(), GetSuperAdminLockKey(), cancellationToken);
    }

    /// <summary>
    /// 获取普通用户权限缓存全局版本；不存在时在分布式锁内创建永久版本。
    /// </summary>
    /// <param name="database">Redis 数据库。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>普通用户权限缓存全局版本。</returns>
    private async Task<string> GetOrCreateVersionAsync(
        StackExchange.Redis.IDatabase database,
        CancellationToken cancellationToken
    )
    {
        var version = await database.StringGetAsync(GetVersionKey());
        cancellationToken.ThrowIfCancellationRequested();
        if (version.HasValue)
            return version.ToString();

        string? createdVersion = null;
        await redisCacheService.LockAsync(
            GetVersionLockKey(),
            async lockCancellationToken =>
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lockCancellationToken
                );
                var operationCancellationToken = operationCts.Token;
                operationCancellationToken.ThrowIfCancellationRequested();
                var currentVersion = await database.StringGetAsync(GetVersionKey());
                if (currentVersion.HasValue)
                {
                    createdVersion = currentVersion.ToString();
                    return;
                }

                createdVersion = Guid.CreateVersion7().ToString();
                await database.StringSetAsync(GetVersionKey(), createdVersion);
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: LockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockExpiration,
            executionTimeout: LockExpiration,
            cancellationToken: cancellationToken
        );
        return createdVersion ?? throw new InvalidOperationException("用户权限缓存版本初始化失败");
    }

    /// <summary>
    /// 在指定缓存锁内删除缓存键。
    /// </summary>
    /// <param name="cacheKey">缓存键。</param>
    /// <param name="lockKey">缓存锁键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task DeleteCacheAsync(string cacheKey, string lockKey, CancellationToken cancellationToken)
    {
        // 缓存删除由调用方通过异步队列执行，失败时最多重试三次；因此这里允许锁获取超时，
        // 不要求删除的锁等待时间必须大于缓存加载的锁内执行上限。
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
                await database.KeyDeleteAsync(cacheKey);
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: LockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockExpiration,
            executionTimeout: LockExpiration,
            cancellationToken: cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<bool> HasAsync(
        Guid userId,
        IReadOnlyCollection<string> permissionCodes,
        PermissionMatchMode matchMode,
        CancellationToken cancellationToken
    )
    {
        var userPermissionCodes = await GetCodesAsync(userId, cancellationToken);
        var userPermissionCodeSet = userPermissionCodes.ToHashSet(StringComparer.Ordinal);
        return matchMode == PermissionMatchMode.All
            ? permissionCodes.All(userPermissionCodeSet.Contains)
            : permissionCodes.Any(userPermissionCodeSet.Contains);
    }
}
