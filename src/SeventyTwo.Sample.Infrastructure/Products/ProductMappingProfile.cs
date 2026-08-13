using Mapster;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
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
            .ConstructUsing(source => new Product(source.Id, source.Name, source.Price))
            .AfterMapping((source, destination) => source.AggregateRootToEntity(destination));
        config
            .NewConfig<Product, ProductRecord>()
            // 新增记录的审计字段和并发版本由持久化层生成。
            .Map(destination => destination.CreatedBy, _ => SystemIds.System)
            .Map(destination => destination.CreatedAt, _ => DateTimeExtension.Now())
            .Map(destination => destination.OrgId, _ => Guid.Empty)
            .Map(destination => destination.Version, _ => Guid.CreateVersion7());
    }
}
