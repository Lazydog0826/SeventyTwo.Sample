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
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!PermissionPolicy.TryParse(policyName, out var matchMode, out var permissionCodes))
        {
            return base.GetPolicyAsync(policyName);
        }

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
public sealed class PermissionAuthorizationHandler(IUserPermissionChecker permissionChecker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;
        if (
            await permissionChecker.HasAsync(
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

public sealed record PermissionRequirement(IReadOnlyCollection<string> PermissionCodes, PermissionMatchMode MatchMode)
    : IAuthorizationRequirement;

internal static class PermissionPolicy
{
    private const string Prefix = "Permission:";

    public static string CreateName(PermissionMatchMode matchMode, IReadOnlyCollection<string> permissionCodes)
    {
        if (!Enum.IsDefined(matchMode))
        {
            throw new ArgumentOutOfRangeException(nameof(matchMode));
        }

        var codes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return codes.Length == 0
            ? throw new ArgumentException("至少需要一个权限编码", nameof(permissionCodes))
            : $"{Prefix}{matchMode}:{string.Join(',', codes.Select(Uri.EscapeDataString))}";
    }

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

        var codes = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
        if (codes.Length == 0)
        {
            return false;
        }

        permissionCodes = codes;
        return true;
    }
}
