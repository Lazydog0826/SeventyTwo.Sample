using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Users;

[AutofacDependency(typeof(IUserRepository))]
public sealed class UserRepository(ISqlSugarClient db) : IUserRepository
{
    public async Task AcquireSecurityLockAsync(Guid id, CancellationToken cancellationToken)
    {
        await db.Ado.ExecuteCommandAsync(
            "SELECT pg_advisory_xact_lock(hashtextextended(@userId, 0))",
            new { userId = id.ToString() },
            cancellationToken
        );
    }

    public async Task<User?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Queryable<UserAccountRecord>()
            .Where(x => x.Id == id && x.DeleteAt == null)
            .FirstAsync(cancellationToken);
        return user?.Adapt<User>();
    }

    public async Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken)
    {
        var user = await db.Queryable<UserAccountRecord>()
            .Where(x => x.Username == account && x.DeleteAt == null)
            .FirstAsync(cancellationToken);
        return user?.Adapt<User>();
    }

    public async Task<IReadOnlyList<User>> GetListAsync(CancellationToken cancellationToken)
    {
        var records = await db.Queryable<UserAccountRecord>()
            .Where(x => x.DeleteAt == null && x.Username != SystemUsernames.SuperAdmin)
            .OrderBy(x => x.CreatedAt)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return records.Adapt<List<User>>();
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
        db.Queryable<UserAccountRecord>()
            .Where(x => x.Username == username && x.DeleteAt == null)
            .AnyAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        var record = new UserAccountRecord
        {
            Id = user.Id,
            Username = user.Username,
            PasswordHash = user.PasswordHash,
            DisplayName = user.DisplayName,
            Phone = user.Phone,
            Email = user.Email,
            DefaultPageId = user.DefaultPageId,
            Enable = user.Enable,
            CreatedBy = SystemIds.System,
            CreatedAt = DateTimeExtension.Now(),
            OrgId = user.OrgId,
            Version = Guid.CreateVersion7(),
        };
        var affectedRows = await db.Insertable(record)
            .PostgreSQLConflictNothing(["username"])
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            throw new UserDomainException(MessageKeys.Users.UsernameExists, DomainErrorType.Conflict);
        }
        record.AggregateRootToEntity(user);
    }

    public async Task SaveAsync(User user, CancellationToken cancellationToken)
    {
        var nextVersion = Guid.CreateVersion7();
        var record = new UserAccountRecord
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Phone = user.Phone,
            Email = user.Email,
            DefaultPageId = user.DefaultPageId,
            Enable = user.Enable,
            OrgId = user.OrgId,
            UpdatedBy = user.UpdatedBy,
            UpdatedAt = user.UpdatedAt,
            Version = nextVersion,
        };
        var affectedRows = await db.Updateable(record)
            .UpdateColumns(x => new
            {
                x.DisplayName,
                x.Phone,
                x.Email,
                x.DefaultPageId,
                x.Enable,
                x.OrgId,
                x.UpdatedBy,
                x.UpdatedAt,
                x.Version,
            })
            .Where(x => x.Id == user.Id && x.Version == user.Version && x.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            if (await GetAsync(user.Id, cancellationToken) is not null)
            {
                throw new UserDomainException(MessageKeys.Users.DataChanged, DomainErrorType.Conflict);
            }
            throw new UserDomainException(MessageKeys.Users.NotFound, DomainErrorType.NotFound);
        }
        user.Version = nextVersion;
    }

    public async Task SavePasswordAsync(User user, CancellationToken cancellationToken)
    {
        var nextVersion = Guid.CreateVersion7();
        var record = new UserAccountRecord
        {
            Id = user.Id,
            PasswordHash = user.PasswordHash,
            UpdatedBy = user.UpdatedBy,
            UpdatedAt = user.UpdatedAt,
            Version = nextVersion,
        };
        var affectedRows = await db.Updateable(record)
            .UpdateColumns(x => new { x.PasswordHash, x.UpdatedBy, x.UpdatedAt, x.Version })
            .Where(x => x.Id == user.Id && x.Version == user.Version && x.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            if (await GetAsync(user.Id, cancellationToken) is not null)
                throw new UserDomainException(MessageKeys.Users.DataChanged, DomainErrorType.Conflict);
            throw new UserDomainException(MessageKeys.Users.NotFound, DomainErrorType.NotFound);
        }
        user.Version = nextVersion;
    }

    public async Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken)
    {
        if (await db.Queryable<UserPermissionRecord>().Where(x => x.UserId == id).AnyAsync(cancellationToken))
        {
            throw new UserDomainException(MessageKeys.Users.HasPermissions, DomainErrorType.Conflict);
        }
        var affectedRows = await db.Deleteable<UserAccountRecord>()
            .Where(x => x.Id == id && x.Version == version && x.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows != 0)
            return;
        if (await GetAsync(id, cancellationToken) is not null)
        {
            throw new UserDomainException(MessageKeys.Users.DataChanged, DomainErrorType.Conflict);
        }
        throw new UserDomainException(MessageKeys.Users.NotFound, DomainErrorType.NotFound);
    }
}
