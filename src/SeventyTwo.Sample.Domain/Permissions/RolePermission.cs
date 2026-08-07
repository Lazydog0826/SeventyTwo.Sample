// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class RolePermission : AggregateRoot
{
    private RolePermission() { }

    public RolePermission(Guid id, Guid roleId, Guid permissionId)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("角色权限 ID 不能为空");
        }

        if (roleId == Guid.Empty)
        {
            throw new PermissionDomainException("角色 ID 不能为空");
        }

        if (permissionId == Guid.Empty)
        {
            throw new PermissionDomainException("权限 ID 不能为空");
        }

        Id = id;
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }
}
