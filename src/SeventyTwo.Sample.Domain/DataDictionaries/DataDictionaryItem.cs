// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.DataDictionaries;

/// <summary>
/// 数据字典项实体。
/// </summary>
public sealed class DataDictionaryItem
{
    /// <summary>
    /// 初始化空的数据字典项实例。
    /// </summary>
    private DataDictionaryItem() { }

    /// <summary>
    /// 初始化数据字典项。
    /// </summary>
    /// <param name="id">数据字典项 ID。</param>
    /// <param name="dictionaryId">所属数据字典 ID。</param>
    /// <param name="value">数据字典项值。</param>
    /// <param name="label">数据字典项标签。</param>
    /// <param name="sortOrder">排序序号。</param>
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

        Id = id;
        DictionaryId = dictionaryId;
        Update(value, label, sortOrder);
    }

    /// <summary>
    /// 数据字典项 ID。
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// 所属数据字典 ID。
    /// </summary>
    public Guid DictionaryId { get; private set; }

    /// <summary>
    /// 数据字典项值。
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// 数据字典项标签。
    /// </summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>
    /// 排序序号。
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 更新数据字典项的值、标签和排序序号。
    /// </summary>
    /// <param name="value">数据字典项值。</param>
    /// <param name="label">数据字典项标签。</param>
    /// <param name="sortOrder">排序序号。</param>
    internal void Update(string value, string label, int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.ItemSortMustNotBeNegative);
        }

        Value = NormalizeValue(value);
        Label = RequireText(label, MessageKeys.DataDictionaries.ItemLabelRequired);
        SortOrder = sortOrder;
    }

    /// <summary>
    /// 校验并规范化数据字典项值。
    /// </summary>
    /// <param name="value">待处理的数据字典项值。</param>
    /// <returns>去除首尾空白后的数据字典项值。</returns>
    internal static string NormalizeValue(string value) =>
        RequireText(value, MessageKeys.DataDictionaries.ItemValueRequired);

    /// <summary>
    /// 校验并规范化必填文本。
    /// </summary>
    /// <param name="value">待处理的文本。</param>
    /// <param name="message">校验失败时使用的错误消息。</param>
    /// <returns>去除首尾空白后的文本。</returns>
    private static string RequireText(string value, string message)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new DataDictionaryDomainException(message) : value.Trim();
    }
}
