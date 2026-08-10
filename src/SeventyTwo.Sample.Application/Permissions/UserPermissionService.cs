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
                cachedValue = permissionCodes;
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
            var cachedValues = await database.SetMembersAsync(cacheKey);
            if (cachedValues.All(value => value != LoadedMarker))
            {
                return null;
            }

            return [.. cachedValues.Where(value => value != LoadedMarker).Select(value => value.ToString())];
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var database = redisCacheService.GetDatabase();
        var isSuperAdmin = await userInfoCacheService.IsSuperAdminAsync(userId, cancellationToken);
        var cacheKey = isSuperAdmin ? GetSuperAdminCacheKey() : GetCacheKey(userId);
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
        var userPermissionCodes = await GetCodesAsync(userId, cancellationToken);
        var userPermissionCodeSet = userPermissionCodes.ToHashSet(StringComparer.Ordinal);
        return matchMode == PermissionMatchMode.All
            ? permissionCodes.All(userPermissionCodeSet.Contains)
            : permissionCodes.Any(userPermissionCodeSet.Contains);
    }
}
