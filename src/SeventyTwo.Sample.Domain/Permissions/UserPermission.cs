// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class UserPermission : AggregateRoot
{
    private UserPermission() { }

    public UserPermission(Guid id, Guid userId, Guid permissionId)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("用户权限 ID 不能为空");
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
        UserId = userId;
        PermissionId = permissionId;
    }

    /// <summary>
    /// 用户 ID。
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 权限 ID。
    /// </summary>
    public Guid PermissionId { get; private set; }
}
