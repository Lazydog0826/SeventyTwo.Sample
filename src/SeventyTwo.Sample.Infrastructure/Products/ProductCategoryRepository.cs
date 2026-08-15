using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;
using SqlSugar;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SeventyTwo.Sample.Infrastructure.Products;

[AutofacDependency(typeof(IProductCategoryRepository))]
public sealed class ProductCategoryRepository(ISqlSugarClient db) : IProductCategoryRepository
{
    /// <summary>
    /// 类目树变更互斥锁键（"PRODCATG"），与机构模块的锁键区分。
    /// </summary>
    private const long MutationLockKey = 0x50524F4443415447;

    /// <inheritdoc />
    public async Task AcquireMutationLockAsync(CancellationToken cancellationToken)
    {
        await db.Ado.ExecuteCommandAsync(
            "SELECT pg_advisory_xact_lock(@lockKey)",
            new { lockKey = MutationLockKey },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<ProductCategory?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Queryable<ProductCategoryRecord>()
            .Where(category => category.Id == id && category.DeleteAt == null)
            .FirstAsync(cancellationToken);
        return record?.Adapt<ProductCategory>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductCategory>> GetListAsync(CancellationToken cancellationToken)
    {
        var records = await db.Queryable<ProductCategoryRecord>()
            .Where(category => category.DeleteAt == null)
            .OrderBy(category => category.SortOrder)
            .OrderBy(category => category.Id)
            .ToListAsync(cancellationToken);
        return records.Adapt<List<ProductCategory>>();
    }

    /// <inheritdoc />
    public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Queryable<ProductCategoryRecord>()
            .Where(category => category.ParentId == id && category.DeleteAt == null)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken)
    {
        var record = new ProductCategoryRecord
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            SortOrder = category.SortOrder,
            Path = category.Path,
            CreatedBy = SystemIds.System,
            Version = Guid.CreateVersion7(),
        };
        await db.Insertable(record).ExecuteCommandAsync(cancellationToken);
        record.AggregateRootToEntity(category);
    }

    /// <inheritdoc />
    public async Task SaveAsync(ProductCategory category, CancellationToken cancellationToken)
    {
        var persisted = await db.Queryable<ProductCategoryRecord>()
            .Where(entity => entity.Id == category.Id && entity.DeleteAt == null)
            .FirstAsync(cancellationToken);
        if (persisted is null)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.NotFound, DomainErrorType.NotFound);
        }

        var oldPath = persisted.Path;
        // 类目为软删除：Delete() 标记后按删除分支落库，不再级联更新后代路径。
        var isDelete = category.DeleteAt.HasValue;
        var nextVersion = Guid.CreateVersion7();
        int affectedRows;
        if (isDelete)
        {
            // 删除分支不携带审计字段，维持 SetColumns 表达式更新。
            affectedRows = await db.Updateable<ProductCategoryRecord>()
                .SetColumns(x => new ProductCategoryRecord
                {
                    Enable = false,
                    DeleteBy = category.DeleteBy,
                    DeleteAt = category.DeleteAt,
                    Version = nextVersion,
                })
                .Where(x => x.Id == category.Id && x.Version == category.Version && x.DeleteAt == null)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            // 更新分支使用实体加 UpdateColumns 风格，修改人与修改时间由公共字段拦截器自动填充。
            var record = new ProductCategoryRecord
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId,
                SortOrder = category.SortOrder,
                Path = category.Path,
                UpdatedBy = category.UpdatedBy,
                UpdatedAt = category.UpdatedAt,
                Version = nextVersion,
            };
            affectedRows = await db.Updateable(record)
                .UpdateColumns(x => new
                {
                    x.Name,
                    x.ParentId,
                    x.SortOrder,
                    x.Path,
                    x.UpdatedBy,
                    x.UpdatedAt,
                    x.Version,
                })
                .Where(x => x.Id == category.Id && x.Version == category.Version && x.DeleteAt == null)
                .ExecuteCommandAsync(cancellationToken);

            // 拦截器在生成更新时填充了实际落库的修改人、修改时间，回写保持聚合与数据库一致。
            category.UpdatedBy = record.UpdatedBy;
            category.UpdatedAt = record.UpdatedAt;
        }

        if (affectedRows == 0)
        {
            if (!isDelete && await FindAsync(category.Id, cancellationToken) is not null)
            {
                throw new ProductDomainException(MessageKeys.ProductCategories.DataChanged, DomainErrorType.Conflict);
            }

            throw new ProductDomainException(MessageKeys.ProductCategories.NotFound, DomainErrorType.NotFound);
        }

        category.Version = nextVersion;

        if (!isDelete && oldPath != category.Path)
        {
            var descendantPrefix = $"{oldPath}/";
            var descendants = await db.Queryable<ProductCategoryRecord>()
                .Where(entity => entity.Path.StartsWith(descendantPrefix) && entity.DeleteAt == null)
                .ToListAsync(cancellationToken);
            var descendantUpdates = descendants
                .Select(descendant => new ProductCategoryRecord
                {
                    Id = descendant.Id,
                    // Path 段为定长 GUID，oldPath 在后代路径中仅作为前缀出现一次，Replace 即精确替换前缀。
                    Path = descendant.Path.Replace(oldPath, category.Path),
                    Version = Guid.CreateVersion7(),
                    UpdatedBy = category.UpdatedBy,
                    UpdatedAt = category.UpdatedAt,
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
}
