using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SeventyTwo.Sample.Application.Permissions;

namespace SeventyTwo.Sample.WebApi.Authentication;

/// <summary>
/// 声明接口所需权限。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 创建权限授权特性，并将匹配模式和权限编码转换为动态策略名称。
    /// </summary>
    /// <param name="matchMode">多个权限编码之间的匹配模式。</param>
    /// <param name="permissionCodes">访问接口所需的权限编码。</param>
    public PermissionAttribute(PermissionMatchMode matchMode, params string[] permissionCodes)
    {
        Policy = PermissionPolicy.CreateName(matchMode, permissionCodes);
    }
}

/// <summary>
/// 动态创建权限授权策略。
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    /// <summary>
    /// 根据权限策略名称动态构建授权策略；非权限策略仍交由默认策略提供器处理。
    /// </summary>
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!PermissionPolicy.TryParse(policyName, out var matchMode, out var permissionCodes))
        {
            return base.GetPolicyAsync(policyName);
        }

        // 权限接口必须先通过业务 JWT 方案建立身份，再由权限处理器检查用户权限。
        var policy = new AuthorizationPolicyBuilder(BusinessJwtAuthenticationDefaults.Scheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissionCodes, matchMode))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

/// <summary>
/// 执行接口权限校验。
/// </summary>
public sealed class PermissionAuthorizationHandler(IUserPermissionCacheService userPermissionCacheService)
    : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>
    /// 从当前身份读取业务用户 ID，并根据授权要求校验其缓存权限。
    /// </summary>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        // 用户 ID 缺失或格式无效时不满足授权要求，由授权框架返回拒绝结果。
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        // HTTP 请求中沿用客户端断开或请求超时的取消信号；其他授权场景没有请求级取消令牌。
        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;
        if (
            await userPermissionCacheService.HasAsync(
                userId,
                requirement.PermissionCodes,
                requirement.MatchMode,
                cancellationToken
            )
        )
        {
            context.Succeed(requirement);
        }
    }
}

/// <summary>
/// 表示一组待校验的权限编码及其匹配模式。
/// </summary>
/// <param name="PermissionCodes">待校验的权限编码集合。</param>
/// <param name="MatchMode">多个权限编码之间的匹配模式。</param>
public sealed record PermissionRequirement(IReadOnlyCollection<string> PermissionCodes, PermissionMatchMode MatchMode)
    : IAuthorizationRequirement;

/// <summary>
/// 负责权限授权策略名称的生成与解析。
/// </summary>
internal static class PermissionPolicy
{
    private const string Prefix = "Permission:";

    /// <summary>
    /// 将权限匹配模式和权限编码编码为可供 ASP.NET Core 引用的策略名称。
    /// </summary>
    public static string CreateName(PermissionMatchMode matchMode, IReadOnlyCollection<string> permissionCodes)
    {
        if (!Enum.IsDefined(matchMode))
        {
            throw new ArgumentOutOfRangeException(nameof(matchMode));
        }

        // 清理空值和重复项，并保留首次出现的权限编码顺序，使策略名称保持稳定。
        var codes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return codes.Length == 0
            ? throw new ArgumentException("至少需要一个权限编码", nameof(permissionCodes))
            : $"{Prefix}{matchMode}:{string.Join(',', codes.Select(Uri.EscapeDataString))}";
    }

    /// <summary>
    /// 尝试从策略名称解析权限匹配模式和权限编码；不属于权限策略或格式无效时返回 false。
    /// </summary>
    public static bool TryParse(
        string policyName,
        out PermissionMatchMode matchMode,
        out IReadOnlyCollection<string> permissionCodes
    )
    {
        matchMode = default;
        permissionCodes = [];
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = policyName[Prefix.Length..].Split(':', 2);
        if (parts.Length != 2 || !Enum.TryParse(parts[0], out matchMode) || !Enum.IsDefined(matchMode))
        {
            return false;
        }

        // 权限编码在生成名称时经过 URI 转义，此处按逗号拆分后恢复原始内容。
        var codes = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
        if (codes.Length == 0)
        {
            return false;
        }

        permissionCodes = codes;
        return true;
    }
}
