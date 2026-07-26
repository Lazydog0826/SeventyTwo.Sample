using AutoMapper;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

public sealed class ProductMappingProfile : Profile
{
    /// <summary>
    /// 配置商品领域对象到应用层输出的映射。
    /// </summary>
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductOutput>();
    }
}
