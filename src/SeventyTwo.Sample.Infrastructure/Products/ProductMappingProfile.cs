using Mapster;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Infrastructure.Products;

/// <summary>
/// 配置商品聚合与持久化模型之间的映射。
/// </summary>
public sealed class ProductPersistenceMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<ProductRecord, Product>()
            .ConstructUsing(source => new Product(
                source.Id,
                source.Name,
                source.Price,
                source.Code,
                source.Description,
                source.Unit,
                source.CategoryId,
                source.Status
            ))
            .AfterMapping((source, destination) => source.AggregateRootToEntity(destination));
        // 聚合到记录的公共字段走同名映射带入：归属与审计由应用服务在创建时显式赋值，
        // Version 由聚合创建构造函数生成，映射不再另行覆盖。
        config.NewConfig<Product, ProductRecord>();
    }
}
