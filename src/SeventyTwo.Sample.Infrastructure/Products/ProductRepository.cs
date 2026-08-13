using SeventyTwo.InfraKit.Autofac;
using Mapster;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;
using SqlSugar;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SeventyTwo.Sample.Infrastructure.Products;

[AutofacDependency(typeof(IProductRepository))]
public sealed class ProductRepository(ISqlSugarClient db) : IProductRepository
{
    /// <inheritdoc />
    public async Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Queryable<ProductRecord>()
            .Where(x => x.Id == id && x.DeleteAt == null)
            .FirstAsync(cancellationToken);
        return record?.Adapt<Product>();
    }

    /// <inheritdoc />
    public async Task<ProductPage> GetPageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        var query = db.Queryable<ProductRecord>().Where(x => x.DeleteAt == null).OrderByDescending(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((request.Index - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
        return new ProductPage(records.Adapt<List<Product>>(), total);
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
        int affectedRows;
        var isDelete = product.DeleteAt.HasValue;
        var nextVersion = product.Version;
        if (isDelete)
        {
            affectedRows = await db.Updateable<ProductRecord>()
                .SetColumns(x => new ProductRecord
                {
                    Enable = false,
                    DeleteBy = product.DeleteBy,
                    DeleteAt = product.DeleteAt,
                })
                .Where(x => x.Id == product.Id && x.DeleteAt == null)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            nextVersion = Guid.CreateVersion7();
            affectedRows = await db.Updateable<ProductRecord>()
                .SetColumns(x => new ProductRecord
                {
                    Name = product.Name,
                    Price = product.Price,
                    UpdatedBy = product.UpdatedBy,
                    UpdatedAt = product.UpdatedAt,
                    Version = nextVersion,
                })
                .Where(x => x.Id == product.Id && x.Version == product.Version && x.DeleteAt == null)
                .ExecuteCommandAsync(cancellationToken);
        }

        if (affectedRows == 0)
        {
            if (!isDelete && await FindAsync(product.Id, cancellationToken) is not null)
            {
                throw new ProductDomainException(MessageKeys.Products.DataChanged, DomainErrorType.Conflict);
            }

            throw new ProductNotFoundException();
        }

        product.Version = nextVersion;
    }
}
