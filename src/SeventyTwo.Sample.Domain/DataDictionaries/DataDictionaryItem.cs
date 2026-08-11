// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.DataDictionaries;

public sealed class DataDictionaryItem
{
    private DataDictionaryItem() { }

    public DataDictionaryItem(Guid id, Guid dictionaryId, string value, string label, int sortOrder)
    {
        if (id == Guid.Empty)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.ItemIdRequired);
        }

        if (dictionaryId == Guid.Empty)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.IdRequired);
        }

        if (sortOrder < 0)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.ItemSortMustNotBeNegative);
        }

        Id = id;
        DictionaryId = dictionaryId;
        Value = RequireText(value, MessageKeys.DataDictionaries.ItemValueRequired);
        Label = RequireText(label, MessageKeys.DataDictionaries.ItemLabelRequired);
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }

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
