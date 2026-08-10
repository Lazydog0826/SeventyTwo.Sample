using System.Collections.Concurrent;
using System.Reflection;
using SeventyTwo.InfraKit.Cache;
using StackExchange.Redis;

namespace SeventyTwo.Sample.ArchitectureTests;

public class InMemoryRedisDatabase : DispatchProxy
{
    private readonly ConcurrentDictionary<string, RedisValue> stringValues = [];

    public void SetString(string key, RedisValue value)
    {
        stringValues[key] = value;
    }

    public bool StringExists(string key)
    {
        return stringValues.ContainsKey(key);
    }

    public RedisValue GetString(string key)
    {
        return stringValues.TryGetValue(key, out var value) ? value : RedisValue.Null;
    }

    public bool Delete(string key)
    {
        return DeleteKey(key);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        ArgumentNullException.ThrowIfNull(args);

        var key = args[0]?.ToString() ?? throw new InvalidOperationException("Redis 键不能为空");
        return targetMethod.Name switch
        {
            nameof(IDatabaseAsync.StringGetAsync) => Task.FromResult(
                stringValues.TryGetValue(key, out var value) ? value : RedisValue.Null
            ),
            nameof(IDatabaseAsync.StringSetAsync) => SetStringAsync(key, (RedisValue)args[1]!),
            nameof(IDatabaseAsync.KeyDeleteAsync) => Task.FromResult(DeleteKey(key)),
            _ => throw new NotSupportedException($"测试 Redis 未实现 {targetMethod.Name}"),
        };
    }

    private Task<bool> SetStringAsync(string key, RedisValue value)
    {
        SetString(key, value);
        return Task.FromResult(true);
    }

    private bool DeleteKey(string key)
    {
        return stringValues.TryRemove(key, out _);
    }
}

public sealed class FakeRedisCacheService(IDatabase database) : IRedisCacheService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = [];

    public TimeSpan? LastLockAcquireTimeout { get; private set; }

    public IDatabase GetDatabase(int? db = null)
    {
        return database;
    }

    public async Task LockAsync(
        string lockKey,
        Func<CancellationToken, Task> func,
        TimeSpan? timeout = null,
        TimeSpan? renewalInterval = null,
        TimeSpan? renewalDuration = null,
        TimeSpan? executionTimeout = null,
        int? db = null,
        CancellationToken cancellationToken = default
    )
    {
        LastLockAcquireTimeout = timeout;
        var semaphore = locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        if (!await semaphore.WaitAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken))
        {
            throw new TimeoutException($"获取测试 Redis 锁超时：{lockKey}");
        }

        try
        {
            await func(cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
