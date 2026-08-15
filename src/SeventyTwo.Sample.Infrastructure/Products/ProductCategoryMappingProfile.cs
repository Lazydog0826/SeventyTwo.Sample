using Mapster;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Infrastructure.Products;

/// <summary>
/// 配置商品类目持久化实体到聚合根的映射，通过聚合构造函数还原以重放领域校验。
/// </summary>
public sealed class ProductCategoryMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<ProductCategoryRecord, ProductCategory>()
            .ConstructUsing(x => new ProductCategory(x.Id, x.Name, x.ParentId, x.Path))
            .AfterMapping((source, destination) => source.AggregateRootToEntity(destination));
    }
}
