using Mapster;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

public sealed class ProductMappingProfile : IRegister
{
    /// <summary>
    /// 配置商品领域对象到应用层输出的映射。
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, ProductOutput>();
    }
}
