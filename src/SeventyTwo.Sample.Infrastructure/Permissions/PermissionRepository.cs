using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Permissions;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Permissions;

[AutofacDependency(typeof(IPermissionRepository))]
public sealed class PermissionRepository(ISqlSugarClient db) : IPermissionRepository
{
    /// <inheritdoc />
    public async Task<Permission?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Queryable<PermissionRecord>()
            .Where(permission => permission.Id == id && permission.DeleteAt == null)
            .FirstAsync(cancellationToken);
        return record?.Adapt<Permission>();
    }

    /// <inheritdoc />
    public Task<bool> CodeExistsAsync(string code, Guid? excludedId, CancellationToken cancellationToken)
    {
        return db.Queryable<PermissionRecord>()
            .Where(permission => permission.Code == code)
            .WhereIF(excludedId.HasValue, permission => permission.Id != excludedId)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Queryable<PermissionRecord>()
            .Where(permission => permission.ParentId == id && permission.DeleteAt == null)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetUserIdsAsync(Guid permissionId, CancellationToken cancellationToken)
    {
        return await db.Queryable<UserPermissionRecord>()
            .Where(userPermission => userPermission.PermissionId == permissionId)
            .Select(userPermission => userPermission.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Permission permission, CancellationToken cancellationToken)
    {
        var record = new PermissionRecord
        {
            Id = permission.Id,
            Code = permission.Code,
            Title = permission.Title,
            Type = permission.Type,
            Enable = permission.Enable,
            SortOrder = permission.SortOrder,
            Icon = permission.Icon,
            VueComponentPath = permission.VueComponentPath,
            RoutePath = permission.RoutePath,
            RouteName = permission.RouteName,
            ParentId = permission.ParentId,
            MetaData = permission.MetaData,
            CreatedBy = SystemIds.System,
            CreatedAt = DateTimeExtension.Now(),
            OrgId = Guid.Empty,
            Version = Guid.CreateVersion7(),
        };
        await db.Insertable(record).ExecuteCommandAsync(cancellationToken);
        record.AggregateRootToEntity(permission);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Permission permission, CancellationToken cancellationToken)
    {
        var nextVersion = Guid.CreateVersion7();
        var affectedRows = await db.Updateable<PermissionRecord>()
            .SetColumns(permissionRecord => new PermissionRecord
            {
                Code = permission.Code,
                Title = permission.Title,
                Type = permission.Type,
                Enable = permission.Enable,
                SortOrder = permission.SortOrder,
                Icon = permission.Icon,
                VueComponentPath = permission.VueComponentPath,
                RoutePath = permission.RoutePath,
                RouteName = permission.RouteName,
                ParentId = permission.ParentId,
                MetaData = permission.MetaData,
                UpdatedBy = permission.UpdatedBy,
                UpdatedAt = permission.UpdatedAt,
                Version = nextVersion,
            })
            .Where(permissionRecord =>
                permissionRecord.Id == permission.Id
                && permissionRecord.Version == permission.Version
                && permissionRecord.DeleteAt == null
            )
            .ExecuteCommandAsync(cancellationToken);

        if (affectedRows == 0)
        {
            if (await FindAsync(permission.Id, cancellationToken) is not null)
            {
                throw new PermissionDomainException(MessageKeys.Permissions.DataChanged, DomainErrorType.Conflict);
            }

            throw new PermissionDomainException(MessageKeys.Permissions.NotFound, DomainErrorType.NotFound);
        }

        permission.Version = nextVersion;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        // 多表删除的事务边界由应用层 IUnitOfWork 统一管理，仓储只执行数据操作。
        if (await HasChildrenAsync(id, cancellationToken))
        {
            throw new PermissionDomainException("权限存在下级权限，不能删除", DomainErrorType.Conflict);
        }

        await db.Deleteable<UserPermissionRecord>()
            .Where(userPermission => userPermission.PermissionId == id)
            .ExecuteCommandAsync(cancellationToken);
        var affectedRows = await db.Deleteable<PermissionRecord>()
            .Where(permission => permission.Id == id && permission.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.NotFound, DomainErrorType.NotFound);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetListAsync(CancellationToken cancellationToken)
    {
        var records = await db.Queryable<PermissionRecord>()
            .Where(permission => permission.DeleteAt == null)
            .OrderBy(permission => permission.SortOrder)
            .OrderBy(permission => permission.Id)
            .ToListAsync(cancellationToken);
        return records.Adapt<List<Permission>>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken)
    {
        var records = await db.Queryable<PermissionRecord>()
            .Where(permission => permission.Enable && permission.DeleteAt == null)
            .OrderBy(permission => permission.SortOrder)
            .ToListAsync(cancellationToken);
        return records.Adapt<List<Permission>>();
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
}
