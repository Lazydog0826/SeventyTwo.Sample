using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application.Authentication;

namespace SeventyTwo.Sample.WebApi.Authentication;

public static class BusinessJwtAuthenticationDefaults
{
    public const string Scheme = "BusinessBearer";
}

public sealed class BusinessJwtAuthenticationOptions : AuthenticationSchemeOptions;

public sealed class BusinessJwtAuthenticationHandler(
    IOptionsMonitor<BusinessJwtAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenService tokenService,
    IRedisCacheService redisCacheService,
    IOptions<CacheConfiguration> cacheConfiguration
) : AuthenticationHandler<BusinessJwtAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (
            !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var authorization)
            || !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorization.Parameter)
        )
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization.Parameter;
        if (!tokenService.TryValidate(token, out var payload) || payload is not { TokenType: "access" })
        {
            return AuthenticateResult.Fail("访问令牌无效");
        }

        var cacheKey = cacheConfiguration.Value.Data("TOKEN_CACHE_KEY", payload.SessionId.ToString());
        var session = await redisCacheService.GetDatabase().HashGetAsync(cacheKey, ["accessTokenHash", "userId"]);
        if (
            session.Length != 2
            || !string.Equals(session[0].ToString(), GetTokenHash(token), StringComparison.Ordinal)
            || !string.Equals(session[1].ToString(), payload.UserId.ToString(), StringComparison.Ordinal)
        )
        {
            return AuthenticateResult.Fail("访问令牌已失效");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
                new Claim(ClaimTypes.Name, payload.Username),
                new Claim("display_name", payload.DisplayName),
                new Claim("session_id", payload.SessionId.ToString()),
            ],
            Scheme.Name
        );
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        await base.HandleChallengeAsync(properties);
    }

    private static string GetTokenHash(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
