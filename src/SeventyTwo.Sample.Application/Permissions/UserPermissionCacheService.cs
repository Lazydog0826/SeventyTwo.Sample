using System.Text.Json;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Application.Permissions;

[AutofacDependency(typeof(IUserPermissionCacheService))]
public sealed class UserPermissionCacheService(
    IPermissionRepository permissionRepository,
    UserInfoCacheService userInfoCacheService,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : IUserPermissionCacheService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockAcquireTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockExpiration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockRenewalInterval = TimeSpan.FromSeconds(10);

    private string GetSuperAdminCacheKey()
    {
        return cacheConfiguration.Value.Data("Permissions", "UserCodes:SuperAdmin");
    }

    private string GetSuperAdminLockKey()
    {
        return cacheConfiguration.Value.Lock("Permissions", "UserCodes:SuperAdmin");
    }

    private string GetCacheKey(Guid userId)
    {
        return cacheConfiguration.Value.Data("Permissions", $"UserCodes:{userId}");
    }

    private string GetLockKey(Guid userId)
    {
        return cacheConfiguration.Value.Lock("Permissions", $"UserCodes:{userId}");
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

                await database.StringSetAsync(cacheKey, JsonSerializer.Serialize(permissionCodes), CacheExpiration);

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
    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        return DeleteCacheAsync(GetCacheKey(userId), GetLockKey(userId), cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteSuperAdminAsync(CancellationToken cancellationToken)
    {
        return DeleteCacheAsync(GetSuperAdminCacheKey(), GetSuperAdminLockKey(), cancellationToken);
    }

    private async Task DeleteCacheAsync(string cacheKey, string lockKey, CancellationToken cancellationToken)
    {
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
