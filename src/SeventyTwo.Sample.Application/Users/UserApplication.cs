using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain.Users;
using StackExchange.Redis;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户应用服务。
/// </summary>
[AutofacDependency(typeof(IUserApplication))]
public sealed class UserApplication(
    IUserRepository userRepository,
    ITokenService tokenService,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : IUserApplication
{
    private static readonly TimeSpan UserInfoCacheDuration = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public async Task<UserOutput> GetAsync(Guid id)
    {
        var cacheKey = cacheConfiguration.Value.Data("Users", $"Info:{id}");
        var database = redisCacheService.GetDatabase();
        var cachedValue = await database.StringGetAsync(cacheKey);
        if (cachedValue.HasValue)
        {
            var cachedUser = JsonSerializer.Deserialize<UserOutput>(cachedValue.ToString());
            if (cachedUser != null)
            {
                return cachedUser;
            }
        }

        var user = await userRepository.GetAsync(id) ?? throw new UserDomainException("用户不存在");
        var output = user.Adapt<UserOutput>();
        await database.StringSetAsync(cacheKey, JsonSerializer.Serialize(output), UserInfoCacheDuration);
        return output;
    }

    /// <inheritdoc />
    public async Task<LoginOutput> LoginAsync(LoginInput request)
    {
        var user = await userRepository.GetByAccountAsync(request.Account);
        if (user == null)
        {
            throw new UserDomainException("账号或密码错误");
        }

        var valid = new PasswordHasher<string>().VerifyHashedPassword(
            request.Account,
            user.PasswordHash,
            request.Password
        );

        if (valid.Equals(PasswordVerificationResult.Failed))
        {
            throw new UserDomainException("账号或密码错误");
        }

        var sessionId = Guid.CreateVersion7();
        var tokens = tokenService.Generate(user, sessionId);

        // 仅缓存令牌哈希，避免 Redis 数据泄露后令牌可被直接使用。
        var database = redisCacheService.GetDatabase();
        var cacheKey = GetTokenCacheKey(sessionId);
        var transaction = database.CreateTransaction();
        var hashSetTask = transaction.HashSetAsync(
            cacheKey,
            [
                new HashEntry("accessTokenHash", GetTokenHash(tokens.AccessToken)),
                new HashEntry("refreshTokenHash", GetTokenHash(tokens.RefreshToken)),
                new HashEntry("userId", user.Id.ToString()),
            ]
        );
        var keyExpireTask = transaction.KeyExpireAsync(cacheKey, tokens.ExpireTime);
        if (!await transaction.ExecuteAsync())
        {
            throw new InvalidOperationException("保存登录会话失败");
        }

        await Task.WhenAll(hashSetTask, keyExpireTask);

        return tokens.Adapt<LoginOutput>();
    }

    /// <inheritdoc />
    public async Task<LoginOutput> RefreshTokenAsync(string refreshToken)
    {
        if (
            string.IsNullOrWhiteSpace(refreshToken)
            || !tokenService.TryValidate(refreshToken, out var payload)
            || payload is not { TokenType: "refresh" }
        )
        {
            throw new TokenAuthenticationException("刷新令牌无效");
        }

        var user = await userRepository.GetAsync(payload.UserId);
        if (user == null)
        {
            throw new TokenAuthenticationException("刷新令牌无效");
        }

        var tokens = tokenService.Generate(user, payload.SessionId);
        var database = redisCacheService.GetDatabase();
        var cacheKey = GetTokenCacheKey(payload.SessionId);
        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.HashEqual(cacheKey, "refreshTokenHash", GetTokenHash(refreshToken)));
        transaction.AddCondition(Condition.HashEqual(cacheKey, "userId", payload.UserId.ToString()));
        var hashSetTask = transaction.HashSetAsync(
            cacheKey,
            [
                new HashEntry("accessTokenHash", GetTokenHash(tokens.AccessToken)),
                new HashEntry("refreshTokenHash", GetTokenHash(tokens.RefreshToken)),
                new HashEntry("userId", user.Id.ToString()),
            ]
        );
        var keyExpireTask = transaction.KeyExpireAsync(cacheKey, tokens.ExpireTime);
        if (!await transaction.ExecuteAsync())
        {
            throw new TokenAuthenticationException("刷新令牌无效");
        }

        await Task.WhenAll(hashSetTask, keyExpireTask);
        return tokens.Adapt<LoginOutput>();
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string refreshToken)
    {
        if (
            string.IsNullOrWhiteSpace(refreshToken)
            || !tokenService.TryValidate(refreshToken, out var payload)
            || payload is not { TokenType: "refresh" }
        )
        {
            return;
        }

        var database = redisCacheService.GetDatabase();
        var cacheKey = GetTokenCacheKey(payload.SessionId);
        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.HashEqual(cacheKey, "refreshTokenHash", GetTokenHash(refreshToken)));
        transaction.AddCondition(Condition.HashEqual(cacheKey, "userId", payload.UserId.ToString()));
        var deleteTask = transaction.KeyDeleteAsync(cacheKey);
        if (!await transaction.ExecuteAsync())
        {
            return;
        }

        await deleteTask;
    }

    /// <summary>
    /// 获取指定会话的令牌缓存键。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <returns>令牌缓存键。</returns>
    private string GetTokenCacheKey(Guid sessionId)
    {
        return cacheConfiguration.Value.Data("TOKEN_CACHE_KEY", sessionId.ToString());
    }

    /// <summary>
    /// 计算令牌的 SHA-256 哈希值。
    /// </summary>
    /// <param name="token">令牌。</param>
    /// <returns>Base64 编码的令牌哈希值。</returns>
    private static string GetTokenHash(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
