using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Domain.Permissions;
using StackExchange.Redis;

namespace SeventyTwo.Sample.Application.Permissions;

[AutofacDependency(typeof(IUserPermissionService))]
public sealed class UserPermissionService(
    IPermissionRepository permissionRepository,
    UserInfoCacheService userInfoCacheService,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : IUserPermissionService
{
    // 系统保留的权限缓存加载标记，权限编码禁止使用该值。
    private const string LoadedMarker = "__CACHE_LOADED__";
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockAcquireTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockExpiration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockRenewalInterval = TimeSpan.FromSeconds(10);

    private string GetSuperAdminCacheKey()
    {
        return cacheConfiguration.Value.Data("Permissions", "UserCodeSet:SuperAdmin");
    }

    private string GetSuperAdminLockKey()
    {
        return cacheConfiguration.Value.Lock("Permissions", "UserCodeSet:SuperAdmin");
    }

    private string GetCacheKey(Guid userId)
    {
        return cacheConfiguration.Value.Data("Permissions", $"UserCodeSet:{userId}");
    }

    private string GetLockKey(Guid userId)
    {
        return cacheConfiguration.Value.Lock("Permissions", $"UserCodeSet:{userId}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var database = redisCacheService.GetDatabase();
        var isSuperAdmin = await userInfoCacheService.IsSuperAdminAsync(userId, cancellationToken);
        var cacheKey = isSuperAdmin ? GetSuperAdminCacheKey() : GetCacheKey(userId);
        var cachedValue = await GetCacheAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        var lockKey = isSuperAdmin ? GetSuperAdminLockKey() : GetLockKey(userId);
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

                // TODO(BUG): 超级管理员也调用 GetCodesByUserIdAsync，只会查询用户权限关联；没有关联记录时无法获得其应有的全部有效权限，应在超级管理员分支查询 GetAllAsync 的编码。
                var permissionCodes = await permissionRepository.GetCodesByUserIdAsync(
                    userId,
                    operationCancellationToken
                );
                var transaction = database.CreateTransaction();
                var cacheDeleteTask = transaction.KeyDeleteAsync(cacheKey);
                var cacheValues = permissionCodes.Select(x => (RedisValue)x).Append(LoadedMarker).ToArray();
                var setAddTask = transaction.SetAddAsync(cacheKey, cacheValues);
                var setExpireTask = transaction.KeyExpireAsync(cacheKey, SlidingExpiration);
                if (!await transaction.ExecuteAsync())
                {
                    throw new InvalidOperationException("保存用户权限缓存失败");
                }

                await Task.WhenAll(cacheDeleteTask, setAddTask, setExpireTask);
                operationCancellationToken.ThrowIfCancellationRequested();
                // TODO(BUG): 缓存加载成功后没有把 permissionCodes 赋给 cachedValue，首次调用最终仍返回空集合；应回传本次加载结果或重新读取缓存。
            },
            timeout: LockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            renewalDuration: LockExpiration,
            executionTimeout: LockExpiration,
            cancellationToken: cancellationToken
        );

        return cachedValue ?? [];

        async Task<IReadOnlyList<string>?> GetCacheAsync()
        {
            var temCachedValue = await database.SetMembersAsync(cacheKey);
            // TODO(BUG): Redis 键不存在时 SetMembersAsync 返回空数组而非 null，当前实现会把冷缓存误判为已命中；同时命中后会把 LoadedMarker 当作权限编码返回。应以 LoadedMarker 判断是否已加载并在返回值中过滤它。
            return [.. temCachedValue.Select(x => x.ToString())];
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey;
        var database = redisCacheService.GetDatabase();
        if (await userInfoCacheService.IsSuperAdminAsync(userId, cancellationToken))
        {
            cacheKey = GetSuperAdminCacheKey();
        }
        else
        {
            cacheKey = GetCacheKey(userId);
        }

        var lockKey = GetLockKey(userId);
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
            timeout: LockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            renewalDuration: LockExpiration,
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
        // TODO(BUG): HasAsync 直接查询 Redis，冷缓存或过期后不会触发 GetCodesAsync 加载，授权入口会把实际有权限的用户误判为无权限。
        string cacheKey;
        var database = redisCacheService.GetDatabase();
        if (await userInfoCacheService.IsSuperAdminAsync(userId, cancellationToken))
        {
            cacheKey = GetSuperAdminCacheKey();
        }
        else
        {
            cacheKey = GetCacheKey(userId);
        }

        var containsTasks = permissionCodes.Select(code => database.SetContainsAsync(cacheKey, code)).ToArray();
        var results = await Task.WhenAll(containsTasks);
        return matchMode == PermissionMatchMode.All ? results.All(x => x) : results.Any(x => x);
    }
}
