// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class UserRole : AggregateRoot
{
    private UserRole() { }

    public UserRole(Guid id, Guid organizationId, Guid userId, Guid roleId)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("用户角色 ID 不能为空");
        }

        if (organizationId == Guid.Empty)
        {
            throw new PermissionDomainException("机构 ID 不能为空");
        }

        if (userId == Guid.Empty)
        {
            throw new PermissionDomainException("用户 ID 不能为空");
        }

        if (roleId == Guid.Empty)
        {
            throw new PermissionDomainException("角色 ID 不能为空");
        }

        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        RoleId = roleId;
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }
}
