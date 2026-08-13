using System.Text.Json;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;

namespace SeventyTwo.Sample.Application.DataDictionaries;

/// <summary>
/// 按编码缓存已启用字典的业务选项，并通过分布式锁避免缓存击穿。
/// </summary>
[AutofacDependency]
public sealed class DataDictionaryCacheService(
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

    /// <summary>获取缓存中的字典选项；缓存未命中时调用加载器。</summary>
    public async Task<IReadOnlyList<DataDictionaryOptionOutput>?> GetOrLoadAsync(
        string code,
        Func<CancellationToken, Task<IReadOnlyList<DataDictionaryOptionOutput>?>> loader,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redisCacheService.GetDatabase();
        var cacheKey = GetCacheKey(code);
        var cachedResult = await GetCacheAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (cachedResult.Found)
        {
            return cachedResult.Options;
        }

        IReadOnlyList<DataDictionaryOptionOutput>? options = null;
        await redisCacheService.LockAsync(
            GetLockKey(code),
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
                    options = cachedResult.Options;
                    return;
                }

                options = await loader(operationCancellationToken);
                operationCancellationToken.ThrowIfCancellationRequested();
                var value = options is null ? EmptyCacheValue : JsonSerializer.Serialize(options);
                await database.StringSetAsync(cacheKey, value, CacheDuration);
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: LoadLockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockLeaseDuration,
            executionTimeout: LockExecutionTimeout,
            cancellationToken: cancellationToken
        );

        return options;

        async Task<(bool Found, IReadOnlyList<DataDictionaryOptionOutput>? Options)> GetCacheAsync()
        {
            var value = await database.StringGetAsync(cacheKey);
            if (!value.HasValue)
            {
                return (false, null);
            }

            if (value.ToString() == EmptyCacheValue)
            {
                return (true, null);
            }

            try
            {
                var cachedOptions = JsonSerializer.Deserialize<DataDictionaryOptionOutput[]>(value.ToString());
                return cachedOptions is null ? (false, null) : (true, cachedOptions);
            }
            catch (JsonException)
            {
                return (false, null);
            }
        }
    }

    /// <summary>删除指定编码的字典选项缓存。</summary>
    public async Task DeleteAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redisCacheService.GetDatabase();
        await redisCacheService.LockAsync(
            GetLockKey(code),
            async lockCancellationToken =>
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lockCancellationToken
                );
                var operationCancellationToken = operationCts.Token;
                operationCancellationToken.ThrowIfCancellationRequested();
                await database.KeyDeleteAsync(GetCacheKey(code));
                operationCancellationToken.ThrowIfCancellationRequested();
            },
            acquisitionTimeout: InvalidationLockAcquireTimeout,
            renewalInterval: LockRenewalInterval,
            leaseDuration: LockLeaseDuration,
            executionTimeout: LockExecutionTimeout,
            cancellationToken: cancellationToken
        );
    }

    private string GetCacheKey(string code) => cacheConfiguration.Value.Data("data-dictionaries", $"options:{code}");

    private string GetLockKey(string code) => cacheConfiguration.Value.Lock("data-dictionaries", $"options:{code}");
}
