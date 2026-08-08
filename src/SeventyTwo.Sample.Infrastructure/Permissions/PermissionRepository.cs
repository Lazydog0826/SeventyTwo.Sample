using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Permissions;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Permissions;

[AutofacDependency(typeof(IPermissionRepository))]
public sealed class PermissionRepository(ISqlSugarClient db) : IPermissionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken)
    {
        var records = await db.Queryable<PermissionRecord>()
            .Where(permission => permission.Enable && permission.DeleteAt == null)
            .ToListAsync(cancellationToken);
        return [.. records.Select(ToDomain)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Queryable<UserPermissionRecord, PermissionRecord>(
                (userPermission, permission) =>
                    new JoinQueryInfos(JoinType.Inner, userPermission.PermissionId == permission.Id)
            )
            .Where(
                (userPermission, permission) =>
                    userPermission.UserId == userId
                    && userPermission.Enable
                    && userPermission.DeleteAt == null
                    && permission.Enable
                    && permission.DeleteAt == null
            )
            .Select((userPermission, permission) => permission.Code)
            .ToListAsync(cancellationToken);
    }

    private static Permission ToDomain(PermissionRecord record)
    {
        var permission = new Permission(
            record.Id,
            record.Code,
            record.Title,
            record.Type,
            record.Icon,
            record.VueComponentPath,
            record.RoutePath,
            record.RouteName,
            record.ParentId,
            record.MetaData
        );
        record.AggregateRootToEntity(permission);
        return permission;
    }
}
