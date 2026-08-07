using System.Security.Cryptography;
using System.Text;
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

[AutofacDependency(typeof(IUserApplication))]
public sealed class UserApplication(
    IUserRepository userRepository,
    ITokenService tokenService,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : IUserApplication
{
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

        var database = redisCacheService.GetDatabase();
        await database.HashSetAsync(
            GetTokenCacheKey(sessionId),
            [
                new HashEntry("accessTokenHash", GetTokenHash(tokens.AccessToken)),
                new HashEntry("refreshTokenHash", GetTokenHash(tokens.RefreshToken)),
                new HashEntry("userId", user.Id.ToString()),
            ]
        );

        return tokens.Adapt<LoginOutput>();
    }

    private string GetTokenCacheKey(Guid sessionId)
    {
        return cacheConfiguration.Value.Data("TOKEN_CACHE_KEY", sessionId.ToString());
    }

    private static string GetTokenHash(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
