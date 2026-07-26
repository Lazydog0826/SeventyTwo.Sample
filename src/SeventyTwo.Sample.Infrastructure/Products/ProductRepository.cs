using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.DynamicExpression;
using SeventyTwo.InfraKit.DynamicExpression.Model;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.InfraKit.ShareDto;
using SeventyTwo.Sample.Domain.Products;
using SqlSugar;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SeventyTwo.Sample.Infrastructure.Products;

[AutofacDependency(typeof(IProductRepository))]
public sealed class ProductRepository(ISqlSugarClient db) : IProductRepository
{
    /// <inheritdoc />
    public async Task<Product?> FindAsync(long id, CancellationToken cancellationToken)
    {
        var record = await db.Queryable<ProductRecord>()
            .Where(x => x.Id == id && x.DeleteAt == null)
            .FirstAsync(cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    /// <inheritdoc />
    public async Task<ProductPage> GetPageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        var query = db.Queryable<ProductRecord>().Where(x => x.DeleteAt == null);
        var sorts =
            request.Sort.Count > 0
                ? request.Sort
                :
                [
                    new SortModel
                    {
                        TableAlias = "x",
                        PropName = nameof(ProductRecord.Id),
                        SortType = SortTypeEnum.Desc,
                    },
                ];
        try
        {
            query = query.Where(request.Search).OrderBy(sorts);
        }
        catch (InvalidOperationException exception)
        {
            throw new ProductDomainException(exception.Message);
        }

        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((request.Index - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
        return new ProductPage([.. records.Select(ToDomain)], total);
    }

    /// <inheritdoc />
    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        var record = new ProductRecord
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Enable = true,
            CreatedBy = 0,
            CreatedAt = DateTimeExtension.Now(),
            OrgId = 0,
            Version = 0,
        };
        await db.Insertable(record).ExecuteCommandAsync(cancellationToken);
        product.EntityToAggregateRoot(record);
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
            nextVersion = product.Version + 1;
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
                throw new ProductDomainException("商品数据已变更，请刷新后重试");
            }

            throw new ProductNotFoundException();
        }

        product.Version = nextVersion;
    }

    /// <summary>
    /// 将商品持久化模型转换为领域聚合。
    /// </summary>
    /// <param name="record">商品持久化模型。</param>
    /// <returns>商品聚合。</returns>
    private Product ToDomain(ProductRecord record)
    {
        var product = new Product(record.Id, record.Name, record.Price);
        product.EntityToAggregateRoot(record);
        return product;
    }
}
