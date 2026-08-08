using Mapster;
using SeventyTwo.Sample.Application.Products;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 对象映射配置
/// </summary>
public sealed class ProductMappingProfile : IRegister
{
    /// <summary>
    /// 配置商品接口请求到应用层输入的映射。
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProductRequest, CreateProductInput>();
        config.NewConfig<UpdateProductRequest, UpdateProductInput>();
    }
}
