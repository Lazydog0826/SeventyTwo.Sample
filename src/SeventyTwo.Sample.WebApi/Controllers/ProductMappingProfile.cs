using AutoMapper;
using SeventyTwo.Sample.Application.Products;

namespace SeventyTwo.Sample.WebApi.Controllers;

public sealed class ProductMappingProfile : Profile
{
    /// <summary>
    /// 配置商品接口请求到应用层输入的映射。
    /// </summary>
    public ProductMappingProfile()
    {
        CreateMap<CreateProductRequest, CreateProductInput>();
        CreateMap<UpdateProductRequest, UpdateProductInput>();
    }
}
