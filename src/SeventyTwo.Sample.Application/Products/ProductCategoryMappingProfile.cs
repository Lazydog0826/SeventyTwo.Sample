using Mapster;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

/// <summary>
/// 配置商品类目聚合到应用层输出模型的映射。
/// </summary>
public sealed class ProductCategoryOutputMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProductCategory, ProductCategoryListOutput>();
    }
}
