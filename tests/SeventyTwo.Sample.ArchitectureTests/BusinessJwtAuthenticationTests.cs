using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.WebApi.Authentication;
using StackExchange.Redis;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class BusinessJwtAuthenticationTests
{
    [Fact]
    public async Task AuthenticateAsync_ShouldRejectTokenIssuedBeforeInvalidBefore()
    {
        const long issuedAt = 1_800_000_000;
        var userId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var userTokenCacheService = new RejectingUserTokenCacheService();
        var database = DispatchProxy.Create<IDatabase, InMemoryRedisDatabase>();
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddAuthentication()
            .AddScheme<BusinessJwtAuthenticationOptions, BusinessJwtAuthenticationHandler>(
                BusinessJwtAuthenticationDefaults.Scheme,
                _ => { }
            );
        services.AddSingleton<ITokenService>(
            new FixedTokenService(
                new(
                    userId,
                    "user",
                    "用户",
                    Guid.CreateVersion7(),
                    DataPermissionType.All,
                    "access",
                    sessionId,
                    issuedAt
                )
            )
        );
        services.AddSingleton<IUserTokenCacheService>(userTokenCacheService);
        services.AddSingleton<IRedisCacheService>(new FakeRedisCacheService(database));
        services.AddSingleton(Options.Create(new CacheConfiguration { KeyNamespace = "tests" }));
        await using var serviceProvider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Request.Headers.Authorization = "Bearer access-token";

        var result = await context.AuthenticateAsync(BusinessJwtAuthenticationDefaults.Scheme);

        Assert.False(result.Succeeded);
        Assert.Equal("访问令牌已失效", result.Failure?.Message);
        Assert.Equal((userId, issuedAt), userTokenCacheService.VerifiedToken);
    }

    private sealed class FixedTokenService(TokenPayload payload) : ITokenService
    {
        public TokenPair Generate(SeventyTwo.Sample.Domain.Users.User user, Guid sessionId) =>
            throw new NotSupportedException();

        public bool TryValidate(string token, out TokenPayload? result)
        {
            result = payload;
            return true;
        }
    }

    private sealed class RejectingUserTokenCacheService : IUserTokenCacheService
    {
        public (Guid UserId, long IssuedAt)? VerifiedToken { get; private set; }

        public Task<bool> SaveAsync(
            Guid userId,
            Guid sessionId,
            TokenPair tokens,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<bool> RefreshAsync(
            Guid userId,
            Guid sessionId,
            long issuedAtUnixTimeSeconds,
            string refreshToken,
            TokenPair tokens,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            Guid userId,
            Guid sessionId,
            string refreshToken,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<bool> SetInvalidBeforeAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> IsTokenIssuedAfterInvalidBeforeAsync(
            Guid userId,
            long issuedAtUnixTimeSeconds,
            CancellationToken cancellationToken
        )
        {
            VerifiedToken = (userId, issuedAtUnixTimeSeconds);
            return Task.FromResult(false);
        }
    }
}
