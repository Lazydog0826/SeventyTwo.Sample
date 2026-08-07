// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class UserPermission : AggregateRoot
{
    private UserPermission() { }

    public UserPermission(Guid id, Guid organizationId, Guid userId, Guid permissionId)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("用户权限 ID 不能为空");
        }

        if (organizationId == Guid.Empty)
        {
            throw new PermissionDomainException("机构 ID 不能为空");
        }

        if (userId == Guid.Empty)
        {
            throw new PermissionDomainException("用户 ID 不能为空");
        }

        if (permissionId == Guid.Empty)
        {
            throw new PermissionDomainException("权限 ID 不能为空");
        }

        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        PermissionId = permissionId;
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid PermissionId { get; private set; }
}
