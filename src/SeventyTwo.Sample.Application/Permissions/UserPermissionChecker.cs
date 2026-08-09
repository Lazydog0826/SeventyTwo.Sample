using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Domain.Permissions;
using StackExchange.Redis;

namespace SeventyTwo.Sample.Application.Permissions;

[AutofacDependency(typeof(IUserPermissionChecker))]
public sealed class UserPermissionChecker(
    IPermissionRepository permissionRepository,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : IUserPermissionChecker
{
    // 系统保留的权限缓存加载标记，权限编码禁止使用该值。
    private const string LoadedMarker = "__CACHE_LOADED__";
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockAcquireTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockExpiration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockRenewalInterval = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public async Task InvalidateAsync(IReadOnlyCollection<Guid> userIds)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var database = redisCacheService.GetDatabase();
        var keys = userIds
            .Distinct()
            .Select(userId => (RedisKey)cacheConfiguration.Value.Data("Permissions", $"UserCodeSet:{userId}"))
            .ToArray();
        await database.KeyDeleteAsync(keys);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (database, cacheKey) = await EnsureLoadedAsync(userId, cancellationToken);
        var members = await database.SetMembersAsync(cacheKey);
        return [.. members.Where(x => x != LoadedMarker).Select(x => x.ToString())];
    }

    /// <inheritdoc />
    public async Task<bool> HasAsync(
        Guid userId,
        IReadOnlyCollection<string> permissionCodes,
        PermissionMatchMode matchMode,
        CancellationToken cancellationToken
    )
    {
        if (permissionCodes.Count == 0)
        {
            return false;
        }

        var (database, cacheKey) = await EnsureLoadedAsync(userId, cancellationToken);
        var containsTasks = permissionCodes.Select(code => database.SetContainsAsync(cacheKey, code)).ToArray();
        var results = await Task.WhenAll(containsTasks);
        return matchMode == PermissionMatchMode.All ? results.All(x => x) : results.Any(x => x);
    }

    private async Task<(IDatabase Database, string CacheKey)> EnsureLoadedAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = cacheConfiguration.Value.Data("Permissions", $"UserCodeSet:{userId}");
        var database = redisCacheService.GetDatabase();
        if (await database.SetContainsAsync(cacheKey, LoadedMarker))
        {
            await database.KeyExpireAsync(cacheKey, SlidingExpiration);
            return (database, cacheKey);
        }

        var lockKey = cacheConfiguration.Value.Data("Permissions", $"UserCodeSetLock:{userId}");
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

                if (await database.SetContainsAsync(cacheKey, LoadedMarker))
                {
                    await database.KeyExpireAsync(cacheKey, SlidingExpiration);
                    operationCancellationToken.ThrowIfCancellationRequested();
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
        return (database, cacheKey);
    }
}
