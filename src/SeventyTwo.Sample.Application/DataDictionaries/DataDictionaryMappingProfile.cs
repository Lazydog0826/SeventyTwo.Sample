using Mapster;
using SeventyTwo.Sample.Domain.DataDictionaries;

namespace SeventyTwo.Sample.Application.DataDictionaries;

public sealed class DataDictionaryMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<DataDictionary, DataDictionaryListOutput>()
            .Map(output => output.ItemCount, dictionary => dictionary.Items.Count);
        config.NewConfig<DataDictionaryItem, DataDictionaryItemOutput>();
        config.NewConfig<DataDictionaryItem, DataDictionaryOptionOutput>();
        config
            .NewConfig<DataDictionary, DataDictionaryItemsOutput>()
            .Map(output => output.DictionaryId, dictionary => dictionary.Id)
            .Map(
                output => output.Items,
                dictionary => dictionary.Items.OrderBy(item => item.SortOrder).ThenBy(item => item.Id)
            );
    }
}
