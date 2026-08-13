using System.Security.Claims;
using System.Text.Json;
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
        if (permissionCodes is null || permissionCodes.Length == 0)
        {
            throw new ArgumentException("必须指定至少一个权限代码", nameof(permissionCodes));
        }

        if (matchMode is not PermissionMatchMode.All and not PermissionMatchMode.Any)
        {
            throw new ArgumentOutOfRangeException(nameof(matchMode), "权限匹配模式必须是 All 或 Any");
        }
        Policy = JsonSerializer.Serialize(
            new PermissionAttributeData { MatchMode = matchMode, PermissionCodes = [.. permissionCodes] }
        );
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
        // 规范：权限策略名只能由 PermissionAttribute 生成，不得手写 JSON 策略名；
        // 可反序列化的空 PermissionCodes 在 All 模式下会按空集匹配成功。
        PermissionAttributeData? permissionAttributeData;
        try
        {
            permissionAttributeData = JsonSerializer.Deserialize<PermissionAttributeData>(policyName);
        }
        catch
        {
            permissionAttributeData = null;
        }
        if (permissionAttributeData == null)
        {
            return base.GetPolicyAsync(policyName);
        }

        // 权限接口必须先通过业务 JWT 方案建立身份，再由权限处理器检查用户权限。
        var policy = new AuthorizationPolicyBuilder(BusinessJwtAuthenticationDefaults.Scheme)
            .RequireAuthenticatedUser()
            .AddRequirements(
                new PermissionRequirement(permissionAttributeData.PermissionCodes, permissionAttributeData.MatchMode)
            )
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

internal sealed class PermissionAttributeData
{
    public PermissionMatchMode MatchMode { get; init; }

    public List<string> PermissionCodes { get; init; } = [];
}
