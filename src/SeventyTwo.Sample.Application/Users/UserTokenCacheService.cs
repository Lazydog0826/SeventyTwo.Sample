using System.Globalization;
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
    IOptions<CacheConfiguration> cacheConfiguration,
    IOptions<TokenLifetimeConfiguration> tokenLifetimeConfiguration
) : IUserTokenCacheService
{
    private readonly TimeSpan _invalidBeforeExpiration = TimeSpan.FromDays(
        tokenLifetimeConfiguration.Value.RefreshTokenExpirationDays
    );

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
        long issuedAtUnixTimeSeconds,
        string refreshToken,
        TokenPair tokens,
        CancellationToken cancellationToken
    )
    {
        var database = redisCacheService.GetDatabase();
        var cacheKey = GetCacheKey(sessionId);
        var invalidBeforeCacheKey = GetInvalidBeforeCacheKey(userId);
        var invalidBeforeValue = await database.StringGetAsync(invalidBeforeCacheKey);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsTokenIssuedAfterInvalidBefore(issuedAtUnixTimeSeconds, invalidBeforeValue))
        {
            return false;
        }

        var transaction = database.CreateTransaction();
        // 将失效分界时间快照加入事务条件，防止校验通过后发生强制失效，旧 Refresh Token
        // 仍完成轮换并生成晚于失效时间的新 Token。
        transaction.AddCondition(
            invalidBeforeValue.HasValue
                ? Condition.StringEqual(invalidBeforeCacheKey, invalidBeforeValue)
                : Condition.KeyNotExists(invalidBeforeCacheKey)
        );
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

    /// <inheritdoc />
    public async Task<bool> SetInvalidBeforeAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 与 JWT iat 的 NumericDate 秒级精度保持一致。同一秒内新签发的令牌也会被判定失效，
        // 这是为避免强制失效前同秒签发的旧令牌被放行而接受的安全取舍。
        var invalidBefore = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = await redisCacheService
            .GetDatabase()
            .StringSetAsync(GetInvalidBeforeCacheKey(userId), invalidBefore, _invalidBeforeExpiration);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsTokenIssuedAfterInvalidBeforeAsync(
        Guid userId,
        long issuedAtUnixTimeSeconds,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cachedValue = await redisCacheService.GetDatabase().StringGetAsync(GetInvalidBeforeCacheKey(userId));
        cancellationToken.ThrowIfCancellationRequested();
        return !cachedValue.HasValue || IsTokenIssuedAfterInvalidBefore(issuedAtUnixTimeSeconds, cachedValue);
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

    /// <summary>
    /// 获取指定用户的令牌失效分界时间缓存键。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <returns>令牌失效分界时间缓存键。</returns>
    private string GetInvalidBeforeCacheKey(Guid userId)
    {
        return cacheConfiguration.Value.Data("token-invalid-before", userId.ToString());
    }

    /// <summary>
    /// 根据失效分界时间缓存值判断令牌颁发时间是否有效。
    /// </summary>
    private static bool IsTokenIssuedAfterInvalidBefore(long issuedAtUnixTimeSeconds, RedisValue invalidBeforeValue)
    {
        if (!invalidBeforeValue.HasValue)
        {
            return true;
        }

        // 缓存格式异常时拒绝令牌，避免缓存损坏导致强制失效策略被绕过。
        if (
            !long.TryParse(
                invalidBeforeValue.ToString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var invalidBefore
            )
        )
        {
            return false;
        }

        // 必须严格晚于失效分界时间；等于分界时间的令牌按已失效处理。
        return issuedAtUnixTimeSeconds > invalidBefore;
    }

    private static string GetTokenHash(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
