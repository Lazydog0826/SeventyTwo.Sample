using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.DataDictionaries;

[SugarTable("data_dictionary")]
[SugarIndex("uq_data_dictionary_org_code", nameof(OrgId), OrderByType.Asc, nameof(Code), OrderByType.Asc, true)]
internal sealed class DataDictionaryRecord : BaseEntity
{
    /// <summary>
    /// 数据字典编码。
    /// </summary>
    [SugarColumn(ColumnName = "code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 数据字典名称。
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 数据字典说明。
    /// </summary>
    [SugarColumn(ColumnName = "description", IsNullable = true)]
    public string? Description { get; init; }

    [SugarColumn(IsIgnore = true)]
    public List<DataDictionaryItemRecord> Items { get; init; } = [];
}

[SugarTable("data_dictionary_item")]
[SugarIndex(
    "uq_data_dictionary_item_dictionary_value",
    nameof(DictionaryId),
    OrderByType.Asc,
    nameof(Value),
    OrderByType.Asc,
    true
)]
[SugarIndex(
    "ix_data_dictionary_item_dictionary_sort_order",
    nameof(DictionaryId),
    OrderByType.Asc,
    nameof(SortOrder),
    OrderByType.Asc,
    nameof(Id),
    OrderByType.Asc
)]
internal sealed class DataDictionaryItemRecord
{
    /// <summary>
    /// 数据字典项主键。
    /// </summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, ColumnDataType = "uuid")]
    public Guid Id { get; init; }

    /// <summary>
    /// 所属数据字典 ID。
    /// </summary>
    [SugarColumn(ColumnName = "dictionary_id", ColumnDataType = "uuid")]
    public Guid DictionaryId { get; init; }

    /// <summary>
    /// 数据字典项值。
    /// </summary>
    [SugarColumn(ColumnName = "value")]
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 数据字典项文本。
    /// </summary>
    [SugarColumn(ColumnName = "label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// 数据字典项排序号。
    /// </summary>
    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; init; }
}
