using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Infrastructure.Authentication;

/// <summary>
/// 提供基于 JWT 的签名和加密令牌生成服务。
/// </summary>
/// <param name="options">JWT 配置。</param>
[AutofacDependency(typeof(ITokenService))]
public sealed class JwtTokenService(
    IOptions<JwtConfiguration> options,
    IOptions<TokenLifetimeConfiguration> tokenLifetimeOptions
) : ITokenService
{
    private readonly JwtConfiguration _configuration = options.Value;
    private readonly TokenLifetimeConfiguration _tokenLifetimeConfiguration = tokenLifetimeOptions.Value;
    private readonly SymmetricSecurityKey _signingKey = new(Encoding.UTF8.GetBytes(options.Value.SigningKey));
    private readonly EncryptingCredentials _encryptingCredentials = CreateEncryptingCredentials(
        options.Value.EncryptionKey
    );

    /// <inheritdoc />
    public TokenPair Generate(User user, Guid sessionId)
    {
        var now = DateTime.UtcNow;
        var accessTokenExpireTime = now.Add(
            TimeSpan.FromMinutes(_tokenLifetimeConfiguration.AccessTokenExpirationMinutes)
        );
        var refreshTokenExpireTime = now.Add(TimeSpan.FromDays(_tokenLifetimeConfiguration.RefreshTokenExpirationDays));
        var accessToken = Generate(user, sessionId.ToString(), "access", accessTokenExpireTime);
        var refreshToken = Generate(user, sessionId.ToString(), "refresh", refreshTokenExpireTime);
        return new TokenPair(accessToken, refreshToken, refreshTokenExpireTime);
    }

    /// <inheritdoc />
    public bool TryValidate(string token, out TokenPayload? payload)
    {
        payload = null;

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _configuration.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _configuration.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    TokenDecryptionKey = _encryptingCredentials.Key,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256, SecurityAlgorithms.Aes256CbcHmacSha512],
                },
                out _
            );

            return TryCreatePayload(principal, out payload);
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// 从已通过签名与有效期校验的令牌主体中解析业务载荷。
    /// 逐字段解析，任一 claim 缺失或非法即整体拒绝且不区分失败原因（fail-closed）。
    /// </summary>
    private static bool TryCreatePayload(ClaimsPrincipal principal, out TokenPayload? payload)
    {
        payload = null;

        if (!Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            return false;
        var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
        if (string.IsNullOrWhiteSpace(username))
            return false;
        var displayName = principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
        if (string.IsNullOrWhiteSpace(displayName))
            return false;
        if (!Guid.TryParse(principal.FindFirst("org_id")?.Value, out var orgId))
            return false;
        // 数据权限类型以枚举底层 short 数值存储，解析时校验必须是已定义的枚举值，
        // 防止伪造令牌携带越权数值绕过数据权限过滤。
        if (
            !short.TryParse(
                principal.FindFirst("data_permission_type")?.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var dataPermissionType
            ) || !Enum.IsDefined((DataPermissionType)dataPermissionType)
        )
            return false;
        var tokenType = principal.FindFirst("token_type")?.Value;
        if (string.IsNullOrWhiteSpace(tokenType))
            return false;
        if (!Guid.TryParse(principal.FindFirst("session_id")?.Value, out var sessionId))
            return false;
        if (
            !long.TryParse(
                principal.FindFirst(JwtRegisteredClaimNames.Iat)?.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var issuedAtUnixTimeSeconds
            )
        )
            return false;

        payload = new TokenPayload(
            userId,
            username,
            displayName,
            orgId,
            (DataPermissionType)dataPermissionType,
            tokenType,
            sessionId,
            issuedAtUnixTimeSeconds
        );
        return true;
    }

    /// <summary>
    /// 为指定用户生成指定类型和有效期的 JWT。
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="sessionId"></param>
    /// <param name="tokenType">令牌类型。</param>
    /// <param name="lifetime">令牌有效时长。</param>
    /// <returns>签名并加密后的 JWT 字符串。</returns>
    private string Generate(User user, string sessionId, string tokenType, DateTime lifetime)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim("org_id", user.OrgId.ToString()),
            // 存枚举底层 short 数值而非成员名，枚举成员重命名不会导致已颁发令牌解析语义变化。
            new Claim("data_permission_type", ((short)user.DataPermissionType).ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("token_type", tokenType),
            new Claim("session_id", sessionId),
        };
        var signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _configuration.Issuer,
            Audience = _configuration.Audience,
            NotBefore = now,
            IssuedAt = now,
            Expires = lifetime,
            SigningCredentials = signingCredentials,
            EncryptingCredentials = _encryptingCredentials,
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }

    /// <summary>
    /// 根据 Base64 编码的密钥创建 JWT 加密凭据。
    /// </summary>
    /// <param name="encryptionKey">Base64 编码的 64 字节加密密钥。</param>
    /// <returns>JWT 加密凭据。</returns>
    /// <exception cref="InvalidOperationException">
    /// 密钥不是有效的 Base64 编码，或解码后长度不是 64 字节。
    /// </exception>
    private static EncryptingCredentials CreateEncryptingCredentials(string encryptionKey)
    {
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(encryptionKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("JWT EncryptionKey 必须是 Base64 编码。", exception);
        }

        if (keyBytes.Length != 64)
        {
            throw new InvalidOperationException("JWT EncryptionKey 解码后必须为 64 字节。");
        }

        return new EncryptingCredentials(
            new SymmetricSecurityKey(keyBytes),
            JwtConstants.DirectKeyUseAlg,
            SecurityAlgorithms.Aes256CbcHmacSha512
        );
    }
}
