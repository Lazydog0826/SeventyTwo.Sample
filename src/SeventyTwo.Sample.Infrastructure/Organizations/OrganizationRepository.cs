using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Organizations;
using SeventyTwo.Sample.Infrastructure.Users;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Organizations;

[AutofacDependency(typeof(IOrganizationRepository))]
public sealed class OrganizationRepository(ISqlSugarClient db) : IOrganizationRepository
{
    private const long MutationLockKey = 0x534556454E54594F;

    public async Task AcquireMutationLockAsync(CancellationToken cancellationToken)
    {
        await db.Ado.ExecuteCommandAsync(
            "SELECT pg_advisory_xact_lock(@lockKey)",
            new { lockKey = MutationLockKey },
            cancellationToken
        );
    }

    public async Task<Organization?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Queryable<OrganizationRecord>()
            .Where(organization => organization.Id == id && organization.DeleteAt == null)
            .FirstAsync(cancellationToken);
        return record?.Adapt<Organization>();
    }

    public async Task<IReadOnlyList<Organization>> GetListAsync(CancellationToken cancellationToken)
    {
        var records = await db.Queryable<OrganizationRecord>()
            .Where(organization => organization.DeleteAt == null)
            .OrderBy(organization => organization.SortOrder)
            .OrderBy(organization => organization.Id)
            .ToListAsync(cancellationToken);
        return records.Adapt<List<Organization>>();
    }

    public Task<bool> CodeExistsAsync(Guid orgId, string code, Guid? excludedId, CancellationToken cancellationToken)
    {
        return db.Queryable<OrganizationRecord>()
            .Where(organization =>
                organization.OrgId == orgId && organization.Code == code && organization.DeleteAt == null
            )
            .WhereIF(excludedId.HasValue, organization => organization.Id != excludedId)
            .AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken)
    {
        var record = new OrganizationRecord
        {
            Id = organization.Id,
            Code = organization.Code,
            Name = organization.Name,
            SortOrder = organization.SortOrder,
            Enable = organization.Enable,
            ParentId = organization.ParentId,
            Path = organization.Path,
            CreatedBy = SystemIds.System,
            CreatedAt = DateTimeExtension.Now(),
            OrgId = organization.OrgId,
            Version = Guid.CreateVersion7(),
        };
        var affectedRows = await db.Insertable(record)
            .PostgreSQLConflictNothing(["org_id", "code"])
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.CodeExists, DomainErrorType.Conflict);
        }

        record.AggregateRootToEntity(organization);
    }

    public async Task SaveAsync(Organization organization, CancellationToken cancellationToken)
    {
        var persisted = await db.Queryable<OrganizationRecord>()
            .Where(entity => entity.Id == organization.Id && entity.DeleteAt == null)
            .FirstAsync(cancellationToken);
        if (persisted is null)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.NotFound, DomainErrorType.NotFound);
        }

        var oldPath = persisted.Path;
        var nextVersion = Guid.CreateVersion7();
        var record = new OrganizationRecord
        {
            Id = organization.Id,
            Code = organization.Code,
            Name = organization.Name,
            SortOrder = organization.SortOrder,
            Enable = organization.Enable,
            ParentId = organization.ParentId,
            Path = organization.Path,
            UpdatedBy = organization.UpdatedBy,
            UpdatedAt = organization.UpdatedAt,
            Version = nextVersion,
        };
        var affectedRows = await db.Updateable(record)
            .UpdateColumns(entity => new
            {
                entity.Code,
                entity.Name,
                entity.SortOrder,
                entity.Enable,
                entity.ParentId,
                entity.Path,
                entity.UpdatedBy,
                entity.UpdatedAt,
                entity.Version,
            })
            .Where(entity =>
                entity.Id == organization.Id && entity.Version == organization.Version && entity.DeleteAt == null
            )
            .ExecuteCommandAsync(cancellationToken);

        if (affectedRows == 0)
        {
            if (await FindAsync(organization.Id, cancellationToken) is not null)
            {
                throw new OrganizationDomainException(MessageKeys.Organizations.DataChanged, DomainErrorType.Conflict);
            }

            throw new OrganizationDomainException(MessageKeys.Organizations.NotFound, DomainErrorType.NotFound);
        }

        organization.Version = nextVersion;

        if (oldPath != organization.Path)
        {
            var descendantPrefix = $"{oldPath}/";
            var descendants = await db.Queryable<OrganizationRecord>()
                .Where(entity => entity.Path.StartsWith(descendantPrefix) && entity.DeleteAt == null)
                .ToListAsync(cancellationToken);
            var descendantUpdates = descendants
                .Select(descendant => new OrganizationRecord
                {
                    Id = descendant.Id,
                    // Path 段为定长 GUID，oldPath 在后代路径中仅作为前缀出现一次，Replace 即精确替换前缀。
                    Path = descendant.Path.Replace(oldPath, organization.Path),
                    Version = Guid.CreateVersion7(),
                    UpdatedBy = organization.UpdatedBy,
                    UpdatedAt = organization.UpdatedAt,
                })
                .ToList();
            if (descendantUpdates.Count > 0)
            {
                await db.Updateable(descendantUpdates)
                    .UpdateColumns(entity => new
                    {
                        entity.Path,
                        entity.Version,
                        entity.UpdatedBy,
                        entity.UpdatedAt,
                    })
                    .ExecuteCommandAsync(cancellationToken);
            }
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (
            await db.Queryable<UserAccountRecord>()
                .Where(user => user.OrgId == id && user.DeleteAt == null)
                .AnyAsync(cancellationToken)
        )
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.HasMembers, DomainErrorType.Conflict);
        }

        if (
            await db.Queryable<OrganizationRecord>()
                .Where(organization => organization.ParentId == id && organization.DeleteAt == null)
                .AnyAsync(cancellationToken)
        )
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.HasChildren, DomainErrorType.Conflict);
        }

        var affectedRows = await db.Deleteable<OrganizationRecord>()
            .Where(organization => organization.Id == id && organization.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.NotFound, DomainErrorType.NotFound);
        }
    }
}
