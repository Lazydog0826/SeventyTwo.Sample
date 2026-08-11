// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.Application.DataDictionaries;

public sealed record DataDictionaryListOutput(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool Enable,
    Guid Version,
    int ItemCount
);

public sealed record DataDictionaryItemOutput(Guid Id, string Value, string Label, int SortOrder);

public sealed record DataDictionaryItemsOutput(
    Guid DictionaryId,
    Guid Version,
    IReadOnlyList<DataDictionaryItemOutput> Items
);

public sealed record DataDictionaryItemMutationOutput(Guid DictionaryVersion, DataDictionaryItemOutput? Item);

public sealed record DataDictionaryOptionOutput(string Value, string Label);

public record CreateDataDictionaryInput(string Code, string Name, string? Description, bool Enable);

public sealed record UpdateDataDictionaryInput(string Code, string Name, string? Description, bool Enable, Guid Version)
    : CreateDataDictionaryInput(Code, Name, Description, Enable);

public sealed record CreateDataDictionaryItemInput(
    Guid DictionaryId,
    string Value,
    string Label,
    int SortOrder,
    Guid DictionaryVersion
);

public sealed record UpdateDataDictionaryItemInput(
    Guid DictionaryId,
    Guid Id,
    string Value,
    string Label,
    int SortOrder,
    Guid DictionaryVersion
);

public sealed record DeleteDataDictionaryItemInput(Guid DictionaryId, Guid Id, Guid DictionaryVersion);
