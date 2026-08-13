using System.Reflection;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Application.Users;
using StackExchange.Redis;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class UserTokenCacheServiceTests
{
    [Fact]
    public async Task SetInvalidBeforeAsync_ShouldCacheCurrentUtcUnixTime()
    {
        var (service, redis, userId, cacheKey) = CreateService();
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var result = await service.SetInvalidBeforeAsync(userId, CancellationToken.None);

        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cachedValue = long.Parse(redis.GetString(cacheKey).ToString());
        Assert.True(result);
        Assert.InRange(cachedValue, before, after);
        Assert.Equal(TimeSpan.FromDays(30), redis.LastStringSetExpiry);
    }

    [Fact]
    public async Task IsTokenIssuedAfterInvalidBeforeAsync_ShouldAllowWhenCacheDoesNotExist()
    {
        var (service, _, userId, _) = CreateService();

        var result = await service.IsTokenIssuedAfterInvalidBeforeAsync(
            userId,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CancellationToken.None
        );

        Assert.True(result);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task IsTokenIssuedAfterInvalidBeforeAsync_ShouldCompareUnixSeconds(
        int issuedAtOffsetSeconds,
        bool expected
    )
    {
        var (service, redis, userId, cacheKey) = CreateService();
        const long invalidBefore = 1_800_000_000;
        redis.SetString(cacheKey, invalidBefore);
        var issuedAt = invalidBefore + issuedAtOffsetSeconds;

        var result = await service.IsTokenIssuedAfterInvalidBeforeAsync(userId, issuedAt, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task IsTokenIssuedAfterInvalidBeforeAsync_ShouldRejectMalformedCacheValue()
    {
        var (service, redis, userId, cacheKey) = CreateService();
        redis.SetString(cacheKey, "invalid");

        var result = await service.IsTokenIssuedAfterInvalidBeforeAsync(
            userId,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CancellationToken.None
        );

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRejectTokenIssuedBeforeInvalidBefore()
    {
        var (service, redis, userId, cacheKey) = CreateService();
        const long invalidBefore = 1_800_000_000;
        redis.SetString(cacheKey, invalidBefore);

        var result = await service.RefreshAsync(
            userId,
            Guid.CreateVersion7(),
            invalidBefore,
            "refresh-token",
            new("access-token", "new-refresh-token", DateTime.UtcNow.AddDays(7)),
            CancellationToken.None
        );

        Assert.False(result);
    }

    private static (
        UserTokenCacheService Service,
        InMemoryRedisDatabase Redis,
        Guid UserId,
        string CacheKey
    ) CreateService()
    {
        var userId = Guid.CreateVersion7();
        var database = DispatchProxy.Create<IDatabase, InMemoryRedisDatabase>();
        var redis = (InMemoryRedisDatabase)(object)database;
        var cacheConfiguration = Options.Create(new CacheConfiguration { KeyNamespace = "tests" });
        var cacheKey = cacheConfiguration.Value.Data("token-invalid-before", userId.ToString());
        var tokenLifetimeConfiguration = Options.Create(
            new TokenLifetimeConfiguration { RefreshTokenExpirationDays = 30 }
        );
        var service = new UserTokenCacheService(
            new FakeRedisCacheService(database),
            cacheConfiguration,
            tokenLifetimeConfiguration
        );
        return (service, redis, userId, cacheKey);
    }
}
