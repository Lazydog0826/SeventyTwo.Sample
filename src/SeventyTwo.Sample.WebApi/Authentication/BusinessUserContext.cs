using System.Globalization;
using System.Security.Claims;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.WebApi.Authentication;

/// <summary>
/// 业务用户上下文：身份由调用方显式注入——HTTP 请求在开始时通过 <see cref="FromPrincipal"/> 注入，
/// 系统调用通过 <see cref="Set"/> 注入，注入后同一作用域内复用。
/// 必须注册为 Scoped：注入方与消费方拿到的必须是同一实例，否则注入结果无法传递。
/// </summary>
[AutofacDependency(typeof(IBusinessUserContext), ServiceLifetime = ServiceLifetime.Scoped)]
public sealed class BusinessUserContext : IBusinessUserContext
{
    private (Guid UserId, Guid OrgId, DataPermissionType DataPermissionType)? _identity;

    /// <inheritdoc />
    public Guid UserId => Identity.UserId;

    /// <inheritdoc />
    public Guid OrgId => Identity.OrgId;

    /// <inheritdoc />
    public DataPermissionType DataPermissionType => Identity.DataPermissionType;

    /// <inheritdoc />
    public void FromPrincipal(ClaimsPrincipal user)
    {
        _identity ??= (
            Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Guid.Parse(user.FindFirstValue("org_id")!),
            (DataPermissionType)short.Parse(user.FindFirstValue("data_permission_type")!, CultureInfo.InvariantCulture)
        );
    }

    /// <inheritdoc />
    public void Set(Guid userId, Guid orgId, DataPermissionType dataPermissionType)
    {
        _identity = (userId, orgId, dataPermissionType);
    }

    /// <summary>
    /// 身份未注入时读取属性属于编程错误，直接抛出而非返回默认值，避免空身份进入权限过滤。
    /// </summary>
    private (Guid UserId, Guid OrgId, DataPermissionType DataPermissionType) Identity =>
        _identity ?? throw new InvalidOperationException("业务用户身份尚未注入。");
}
