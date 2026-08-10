using System.Text.Json;
using Mapster;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 提供可复用的用户信息缓存查询。
/// </summary>
[AutofacDependency]
public sealed class UserInfoCacheService(
    IUserRepository userRepository,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LoadLockAcquireTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan InvalidationLockAcquireTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LockRenewalInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockLeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockExecutionTimeout = TimeSpan.FromSeconds(30);
    private const string EmptyCacheValue = "null";
    private const string SuperAdmin = "superadmin";

    /// <summary>
    /// 获取指定用户的缓存键。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <returns>用户信息缓存键。</returns>
    private string GetCacheKey(Guid id)
    {
        return cacheConfiguration.Value.Data("users", $"info:{id}");
    }

    /// <summary>
    /// 获取指定用户的缓存锁键。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <returns>用户信息缓存锁键。</returns>
    private string GetLockKey(Guid id)
    {
        return cacheConfiguration.Value.Lock("users", $"info:{id}");
    }

    /// <summary>
    /// 获取用户信息；用户不存在时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">用于取消锁等待和缓存加载的令牌。</param>
    /// <returns>用户信息；用户不存在时返回 <see langword="null"/>。</returns>
    public async Task<UserOutput?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cacheKey = GetCacheKey(id);
        var database = redisCacheService.GetDatabase();
        var cachedResult = await GetCacheAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (cachedResult.Found)
        {
            return cachedResult.Output;
        }

        UserOutput? output = null;

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
                    output = cachedResult.Output;
                    return;
                }

                var user = await userRepository.GetAsync(id, operationCancellationToken);
                if (user is null)
                {
                    await database.StringSetAsync(cacheKey, EmptyCacheValue, CacheDuration);
                    operationCancellationToken.ThrowIfCancellationRequested();
                    return;
                }

                output = user.Adapt<UserOutput>();
                await database.StringSetAsync(cacheKey, JsonSerializer.Serialize(output), CacheDuration);
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: LoadLockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockLeaseDuration,
            executionTimeout: LockExecutionTimeout,
            cancellationToken: cancellationToken
        );

        return output;

        async Task<(bool Found, UserOutput? Output)> GetCacheAsync()
        {
            var cachedValue = await database.StringGetAsync(cacheKey);
            if (!cachedValue.HasValue)
            {
                return (false, null);
            }

            var serializedValue = cachedValue.ToString();
            if (serializedValue == EmptyCacheValue)
            {
                return (true, null);
            }

            try
            {
                return (true, JsonSerializer.Deserialize<UserOutput>(serializedValue));
            }
            catch (JsonException)
            {
                return (false, null);
            }
        }
    }

    /// <summary>
    /// 删除指定用户的信息缓存。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">用于取消锁等待和缓存删除的令牌。</param>
    public async Task DeleteCacheAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cacheKey = GetCacheKey(id);
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
                await database.KeyDeleteAsync(cacheKey);
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
    /// 判断指定用户是否为超级管理员。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">用于取消用户信息查询的令牌。</param>
    /// <returns>是超级管理员时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public async Task<bool> IsSuperAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        var userInfo = await FindAsync(id, cancellationToken);
        return SuperAdmin == userInfo?.Username;
    }
}
