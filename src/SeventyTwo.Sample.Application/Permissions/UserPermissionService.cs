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
        return cacheConfiguration.Value.Data("Permissions", $"UserCodeSet:SuperAdmin");
    }

    private string GetCacheKey(Guid userId)
    {
        return cacheConfiguration.Value.Data("Permissions", $"UserCodeSet:{userId}");
    }

    private string GetLockKey(Guid userId)
    {
        return cacheConfiguration.Value.Data("Permissions", $"UserCodeSet:{userId}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken)
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
        var cachedValue = await GetCacheAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (cachedValue is not null)
        {
            return cachedValue;
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
                cachedValue = await GetCacheAsync();
                operationCancellationToken.ThrowIfCancellationRequested();
                if (cachedValue is not null)
                {
                    return;
                }
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
