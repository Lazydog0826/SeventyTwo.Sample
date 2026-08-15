using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SeventyTwo.Sample.Infrastructure.Products;

[AutofacDependency(typeof(IProductRepository))]
public sealed class ProductRepository(ISqlSugarClient db) : IProductRepository
{
    /// <inheritdoc />
    public async Task<Product?> FindAsync(
        Guid id,
        DataPermissionScope dataPermissionScope,
        CancellationToken cancellationToken
    )
    {
        var record = await db.Queryable<ProductRecord>()
            .Where(x => x.Id == id && x.DeleteAt == null)
            .ApplyDataPermission(dataPermissionScope)
            .FirstAsync(cancellationToken);
        return record?.Adapt<Product>();
    }

    /// <inheritdoc />
    public async Task<ProductPage> GetPageAsync(
        ProductPageRequest request,
        DataPermissionScope dataPermissionScope,
        CancellationToken cancellationToken
    )
    {
        var keyword = request.Keyword?.Trim().ToLowerInvariant();
        var query = db.Queryable<ProductRecord>()
            .Where(x => x.DeleteAt == null)
            .ApplyDataPermission(dataPermissionScope)
            .WhereIF(
                !string.IsNullOrEmpty(keyword),
                x => x.Name.ToLower().Contains(keyword!) || x.Code.ToLower().Contains(keyword!)
            )
            .WhereIF(request.Status.HasValue, x => x.Status == request.Status)
            .OrderByDescending(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((request.Index - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
        return new ProductPage(records.Adapt<List<Product>>(), total);
    }

    /// <inheritdoc />
    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = db.Queryable<ProductRecord>().Where(x => x.DeleteAt == null && x.Code == code);
        if (excludeId is not null)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        var record = product.Adapt<ProductRecord>();
        await db.Insertable(record).ExecuteCommandAsync(cancellationToken);
        record.AggregateRootToEntity(product);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Product product, CancellationToken cancellationToken)
    {
        var nextVersion = Guid.CreateVersion7();
        var affectedRows = await db.Updateable<ProductRecord>()
            .SetColumns(x => new ProductRecord
            {
                Name = product.Name,
                Price = product.Price,
                Code = product.Code,
                Description = product.Description,
                Unit = product.Unit,
                CategoryId = product.CategoryId,
                Status = product.Status,
                UpdatedBy = product.UpdatedBy,
                UpdatedAt = product.UpdatedAt,
                Version = nextVersion,
            })
            .Where(x => x.Id == product.Id && x.Version == product.Version && x.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);

        if (affectedRows == 0)
        {
            if (await ExistsAsync(product.Id, cancellationToken))
            {
                throw new ProductDomainException(MessageKeys.Products.DataChanged, DomainErrorType.Conflict);
            }

            throw new ProductNotFoundException();
        }

        product.Version = nextVersion;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken)
    {
        var affectedRows = await db.Deleteable<ProductRecord>()
            .Where(x => x.Id == id && x.Version == version && x.DeleteAt == null)
            .ExecuteCommandAsync(cancellationToken);
        if (affectedRows != 0)
        {
            return;
        }

        if (await ExistsAsync(id, cancellationToken))
        {
            throw new ProductDomainException(MessageKeys.Products.DataChanged, DomainErrorType.Conflict);
        }

        throw new ProductNotFoundException();
    }

    /// <summary>
    /// 判断指定未删除商品是否存在；用于乐观锁失败后区分并发冲突与未找到，不做数据权限过滤。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品存在时返回 <see langword="true"/>。</returns>
    private Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Queryable<ProductRecord>().Where(x => x.Id == id && x.DeleteAt == null).AnyAsync(cancellationToken);
    }
}
