// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.DataDictionaries;

public sealed class DataDictionaryItem : AggregateRoot
{
    private DataDictionaryItem() { }

    public DataDictionaryItem(Guid id, Guid dictionaryId, string value, string label, int sortOrder)
    {
        if (id == Guid.Empty)
        {
            throw new DataDictionaryDomainException("数据字典项 ID 不能为空");
        }

        if (dictionaryId == Guid.Empty)
        {
            throw new DataDictionaryDomainException("数据字典 ID 不能为空");
        }

        if (sortOrder < 0)
        {
            throw new DataDictionaryDomainException("数据字典项排序号不能小于 0");
        }

        Id = id;
        DictionaryId = dictionaryId;
        Value = RequireText(value, "数据字典项值不能为空");
        Label = RequireText(label, "数据字典项文本不能为空");
        SortOrder = sortOrder;
    }

    public Guid DictionaryId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    private static string RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DataDictionaryDomainException(message);
        }

        return value.Trim();
    }
}
