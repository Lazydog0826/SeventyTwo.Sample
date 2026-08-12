using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Authentication;
using StackExchange.Redis;

namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 基于 Redis 的用户令牌会话缓存服务。
/// </summary>
[AutofacDependency(typeof(IUserTokenCacheService))]
public sealed class UserTokenCacheService(
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : IUserTokenCacheService
{
    /// <inheritdoc />
    public async Task<bool> SaveAsync(
        Guid userId,
        Guid sessionId,
        TokenPair tokens,
        CancellationToken cancellationToken
    )
    {
        var database = redisCacheService.GetDatabase();
        var cacheKey = GetCacheKey(sessionId);
        var transaction = database.CreateTransaction();
        var hashSetTask = SetTokenHashAsync(transaction, cacheKey, userId, tokens);
        var keyExpireTask = transaction.KeyExpireAsync(cacheKey, tokens.ExpireTime);
        if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken))
        {
            return false;
        }

        await Task.WhenAll(hashSetTask, keyExpireTask).WaitAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RefreshAsync(
        Guid userId,
        Guid sessionId,
        string refreshToken,
        TokenPair tokens,
        CancellationToken cancellationToken
    )
    {
        var database = redisCacheService.GetDatabase();
        var cacheKey = GetCacheKey(sessionId);
        var transaction = database.CreateTransaction();
        AddSessionConditions(transaction, cacheKey, userId, refreshToken);
        var hashSetTask = SetTokenHashAsync(transaction, cacheKey, userId, tokens);
        var keyExpireTask = transaction.KeyExpireAsync(cacheKey, tokens.ExpireTime);
        if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken))
        {
            return false;
        }

        await Task.WhenAll(hashSetTask, keyExpireTask).WaitAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid sessionId,
        string refreshToken,
        CancellationToken cancellationToken
    )
    {
        var database = redisCacheService.GetDatabase();
        var cacheKey = GetCacheKey(sessionId);
        var transaction = database.CreateTransaction();
        AddSessionConditions(transaction, cacheKey, userId, refreshToken);
        var deleteTask = transaction.KeyDeleteAsync(cacheKey);
        if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken))
        {
            return false;
        }

        await deleteTask.WaitAsync(cancellationToken);
        return true;
    }

    private static Task SetTokenHashAsync(ITransaction transaction, RedisKey cacheKey, Guid userId, TokenPair tokens)
    {
        // 仅缓存令牌哈希，避免 Redis 数据泄露后令牌可被直接使用。
        return transaction.HashSetAsync(
            cacheKey,
            [
                new HashEntry("accessTokenHash", GetTokenHash(tokens.AccessToken)),
                new HashEntry("refreshTokenHash", GetTokenHash(tokens.RefreshToken)),
                new HashEntry("userId", userId.ToString()),
            ]
        );
    }

    private static void AddSessionConditions(
        ITransaction transaction,
        RedisKey cacheKey,
        Guid userId,
        string refreshToken
    )
    {
        transaction.AddCondition(Condition.HashEqual(cacheKey, "refreshTokenHash", GetTokenHash(refreshToken)));
        transaction.AddCondition(Condition.HashEqual(cacheKey, "userId", userId.ToString()));
    }

    private string GetCacheKey(Guid sessionId)
    {
        return cacheConfiguration.Value.Data("token-cache-key", sessionId.ToString());
    }

    private static string GetTokenHash(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
