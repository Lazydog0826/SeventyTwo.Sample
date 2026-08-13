using Mapster;
using SeventyTwo.Sample.Domain.DataDictionaries;

namespace SeventyTwo.Sample.Infrastructure.DataDictionaries;

/// <summary>
/// 配置数据字典聚合与持久化模型之间的映射。
/// </summary>
public sealed class DataDictionaryPersistenceMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<DataDictionaryItemRecord, DataDictionaryItem>()
            .ConstructUsing(source => new DataDictionaryItem(
                source.Id,
                source.DictionaryId,
                source.Value,
                source.Label,
                source.SortOrder
            ));
        config.NewConfig<DataDictionaryItem, DataDictionaryItemRecord>();
        config
            .NewConfig<DataDictionaryRecord, DataDictionary>()
            .ConstructUsing(source => new DataDictionary(
                source.Id,
                source.Code,
                source.Name,
                source.Description,
                source.Items.Adapt<List<DataDictionaryItem>>()
            ))
            .AfterMapping((source, destination) => source.AggregateRootToEntity(destination));
    }
}
