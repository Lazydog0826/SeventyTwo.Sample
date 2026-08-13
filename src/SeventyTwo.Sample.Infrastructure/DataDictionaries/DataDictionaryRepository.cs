using System.Data.Common;
using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.DataDictionaries;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.DataDictionaries;

[AutofacDependency(typeof(IDataDictionaryRepository))]
public sealed class DataDictionaryRepository(ISqlSugarClient db) : IDataDictionaryRepository
{
    public async Task<DataDictionary?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Queryable<DataDictionaryRecord>()
            .Where(dictionary => dictionary.Id == id && dictionary.OrgId == Guid.Empty && dictionary.DeleteAt == null)
            .FirstAsync(cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.Items.AddRange(await GetItemRecordsAsync([id], cancellationToken));
        return record.Adapt<DataDictionary>();
    }

    public async Task<DataDictionary?> FindEnabledByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var record = await db.Queryable<DataDictionaryRecord>()
            .Where(dictionary =>
                dictionary.OrgId == Guid.Empty
                && dictionary.Code == code
                && dictionary.Enable
                && dictionary.DeleteAt == null
            )
            .FirstAsync(cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.Items.AddRange(await GetItemRecordsAsync([record.Id], cancellationToken));
        return record.Adapt<DataDictionary>();
    }

    public async Task<DataDictionaryPage> GetPageAsync(
        DataDictionaryPageRequest request,
        CancellationToken cancellationToken
    )
    {
        var keyword = request.Keyword?.Trim().ToLowerInvariant();
        var query = db.Queryable<DataDictionaryRecord>()
            .Where(dictionary => dictionary.OrgId == Guid.Empty && dictionary.DeleteAt == null)
            .WhereIF(
                !string.IsNullOrEmpty(keyword),
                dictionary =>
                    dictionary.Code.ToLower().Contains(keyword!) || dictionary.Name.ToLower().Contains(keyword!)
            )
            .WhereIF(request.Enable.HasValue, dictionary => dictionary.Enable == request.Enable)
            .OrderBy(dictionary => dictionary.CreatedAt)
            .OrderBy(dictionary => dictionary.Id);
        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((request.Index - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
        var items = await GetItemRecordsAsync(records.Select(record => record.Id).ToArray(), cancellationToken);
        var itemsByDictionary = items.GroupBy(item => item.DictionaryId).ToDictionary(group => group.Key);
        foreach (var record in records)
        {
            if (itemsByDictionary.TryGetValue(record.Id, out var dictionaryItems))
            {
                record.Items.AddRange(dictionaryItems);
            }
        }

        return new DataDictionaryPage(records.Adapt<List<DataDictionary>>(), total);
    }

    public Task<bool> CodeExistsAsync(string code, Guid? excludedId, CancellationToken cancellationToken)
    {
        return db.Queryable<DataDictionaryRecord>()
            .Where(dictionary =>
                dictionary.OrgId == Guid.Empty && dictionary.Code == code && dictionary.DeleteAt == null
            )
            .WhereIF(excludedId.HasValue, dictionary => dictionary.Id != excludedId)
            .AnyAsync(cancellationToken);
    }

    public async Task AddAsync(DataDictionary dictionary, CancellationToken cancellationToken)
    {
        var record = new DataDictionaryRecord
        {
            Id = dictionary.Id,
            Code = dictionary.Code,
            Name = dictionary.Name,
            Description = dictionary.Description,
            Enable = dictionary.Enable,
            CreatedBy = SystemIds.System,
            CreatedAt = DateTimeExtension.Now(),
            OrgId = Guid.Empty,
            Version = Guid.CreateVersion7(),
        };
        var affectedRows = await db.Insertable(record)
            .PostgreSQLConflictNothing(["org_id", "code"])
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.CodeExists, DomainErrorType.Conflict);
        }

        record.AggregateRootToEntity(dictionary);
    }

    public async Task SaveAsync(DataDictionary dictionary, CancellationToken cancellationToken)
    {
        dictionary.Version = await UpdateAggregateRecordAsync(dictionary, cancellationToken);
    }

    public async Task SaveItemsAsync(DataDictionary dictionary, CancellationToken cancellationToken)
    {
        var nextVersion = await UpdateAggregateRecordAsync(dictionary, cancellationToken);
        await db.Deleteable<DataDictionaryItemRecord>()
            .Where(item => item.DictionaryId == dictionary.Id)
            .ExecuteCommandAsync(cancellationToken);
        if (dictionary.Items.Count > 0)
        {
            var records = dictionary.Items.Adapt<List<DataDictionaryItemRecord>>();
            await db.Insertable(records).ExecuteCommandAsync(cancellationToken);
        }

        dictionary.Version = nextVersion;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await db.Deleteable<DataDictionaryItemRecord>()
            .Where(item => item.DictionaryId == id)
            .ExecuteCommandAsync(cancellationToken);
        var affectedRows = await db.Deleteable<DataDictionaryRecord>()
            .Where(dictionary => dictionary.Id == id && dictionary.OrgId == Guid.Empty && dictionary.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows == 0)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.NotFound, DomainErrorType.NotFound);
        }
    }

    private async Task<Guid> UpdateAggregateRecordAsync(DataDictionary dictionary, CancellationToken cancellationToken)
    {
        var nextVersion = Guid.CreateVersion7();
        var record = new DataDictionaryRecord
        {
            Id = dictionary.Id,
            Code = dictionary.Code,
            Name = dictionary.Name,
            Description = dictionary.Description,
            Enable = dictionary.Enable,
            UpdatedBy = dictionary.UpdatedBy,
            UpdatedAt = dictionary.UpdatedAt,
            Version = nextVersion,
        };
        int affectedRows;
        try
        {
            affectedRows = await db.Updateable(record)
                .UpdateColumns(entity => new
                {
                    entity.Code,
                    entity.Name,
                    entity.Description,
                    entity.Enable,
                    entity.UpdatedBy,
                    entity.UpdatedAt,
                    entity.Version,
                })
                .Where(entity =>
                    entity.Id == dictionary.Id
                    && entity.OrgId == Guid.Empty
                    && entity.Version == dictionary.Version
                    && entity.DeleteAt == null
                )
                .ExecuteCommandAsync(cancellationToken);
        }
        catch (Exception exception) when (IsCodeConflict(exception))
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.CodeExists, DomainErrorType.Conflict);
        }
        if (affectedRows == 0)
        {
            if (await FindAsync(dictionary.Id, cancellationToken) is not null)
            {
                throw new DataDictionaryDomainException(
                    MessageKeys.DataDictionaries.DataChanged,
                    DomainErrorType.Conflict
                );
            }

            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.NotFound, DomainErrorType.NotFound);
        }

        return nextVersion;
    }

    internal static bool IsCodeConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException { SqlState: "23505" })
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<DataDictionaryItemRecord>> GetItemRecordsAsync(
        Guid[] dictionaryIds,
        CancellationToken cancellationToken
    )
    {
        if (dictionaryIds.Length == 0)
        {
            return [];
        }

        return await db.Queryable<DataDictionaryItemRecord>()
            .Where(item => dictionaryIds.AsEnumerable().Contains(item.DictionaryId))
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }
}
