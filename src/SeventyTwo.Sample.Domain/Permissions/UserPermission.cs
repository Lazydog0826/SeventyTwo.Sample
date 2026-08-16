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
            throw new PermissionDomainException(MessageKeys.Permissions.UserPermissionIdRequired);
        }

        if (userId == Guid.Empty)
        {
            throw new PermissionDomainException(MessageKeys.Users.IdRequired);
        }

        if (permissionId == Guid.Empty)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.IdRequired);
        }

        Id = id;
        Enable = true;
        Version = Guid.CreateVersion7();
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
