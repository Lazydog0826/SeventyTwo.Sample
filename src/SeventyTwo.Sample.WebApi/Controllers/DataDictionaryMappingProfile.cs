using Mapster;
using SeventyTwo.Sample.Application.DataDictionaries;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 数据字典接口对象映射配置。
/// </summary>
public sealed class DataDictionaryMappingProfile : IRegister
{
    /// <summary>
    /// 配置数据字典接口请求到应用层输入的映射。
    /// </summary>
    /// <param name="config">Mapster 映射配置。</param>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateDataDictionaryRequest, CreateDataDictionaryInput>();
        config.NewConfig<UpdateDataDictionaryRequest, UpdateDataDictionaryInput>();
        config.NewConfig<CreateDataDictionaryItemRequest, CreateDataDictionaryItemInput>();
        config.NewConfig<UpdateDataDictionaryItemRequest, UpdateDataDictionaryItemInput>();
        config.NewConfig<DeleteDataDictionaryItemRequest, DeleteDataDictionaryItemInput>();
    }
}
