// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.DataDictionaries;

/// <summary>
/// 数据字典聚合根。
/// </summary>
public sealed class DataDictionary : AggregateRoot
{
    private readonly List<DataDictionaryItem> _items = [];

    /// <summary>
    /// 初始化空的数据字典实例。
    /// </summary>
    private DataDictionary() { }

    /// <summary>
    /// 初始化数据字典。
    /// </summary>
    /// <param name="id">数据字典 ID。</param>
    /// <param name="code">数据字典编码。</param>
    /// <param name="name">数据字典名称。</param>
    /// <param name="description">数据字典描述。</param>
    /// <param name="items">数据字典项集合。</param>
    public DataDictionary(
        Guid id,
        string code,
        string name,
        string? description = null,
        IEnumerable<DataDictionaryItem>? items = null
    )
    {
        if (id == Guid.Empty)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.IdRequired);
        }

        Id = id;
        Enable = true;
        Version = Guid.CreateVersion7();
        Code = RequireText(code, MessageKeys.DataDictionaries.CodeRequired);
        Name = RequireText(name, MessageKeys.DataDictionaries.NameRequired);
        Description = NormalizeOptional(description);
        if (items is not null)
        {
            _items.AddRange(items);
        }
    }

    /// <summary>
    /// 数据字典编码。
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// 数据字典名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 数据字典描述。
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 数据字典项只读集合。
    /// </summary>
    public IReadOnlyList<DataDictionaryItem> Items => _items;

    /// <summary>
    /// 更新数据字典的基本信息和启用状态。
    /// </summary>
    /// <param name="code">数据字典编码。</param>
    /// <param name="name">数据字典名称。</param>
    /// <param name="description">数据字典描述。</param>
    /// <param name="enable">是否启用。</param>
    /// <param name="version">用于并发校验的当前版本。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    public void Update(
        string code,
        string name,
        string? description,
        bool enable,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt
    )
    {
        ValidateMutation(version, updatedAt);
        Code = RequireText(code, MessageKeys.DataDictionaries.CodeRequired);
        Name = RequireText(name, MessageKeys.DataDictionaries.NameRequired);
        Description = NormalizeOptional(description);
        Enable = enable;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 添加数据字典项。
    /// </summary>
    /// <param name="id">数据字典项 ID。</param>
    /// <param name="value">数据字典项值。</param>
    /// <param name="label">数据字典项标签。</param>
    /// <param name="sortOrder">排序序号。</param>
    /// <param name="version">用于并发校验的当前版本。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    /// <returns>新添加的数据字典项。</returns>
    public DataDictionaryItem AddItem(
        Guid id,
        string value,
        string label,
        int sortOrder,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt
    )
    {
        ValidateMutation(version, updatedAt);
        var item = new DataDictionaryItem(id, Id, value, label, sortOrder);
        EnsureItemValueAvailable(item.Value, null);
        _items.Add(item);
        MarkChanged(updatedBy, updatedAt);
        return item;
    }

    /// <summary>
    /// 更新指定的数据字典项。
    /// </summary>
    /// <param name="itemId">数据字典项 ID。</param>
    /// <param name="value">数据字典项值。</param>
    /// <param name="label">数据字典项标签。</param>
    /// <param name="sortOrder">排序序号。</param>
    /// <param name="version">用于并发校验的当前版本。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    public void UpdateItem(
        Guid itemId,
        string value,
        string label,
        int sortOrder,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt
    )
    {
        ValidateMutation(version, updatedAt);
        var item = GetRequiredItem(itemId);
        var normalizedValue = DataDictionaryItem.NormalizeValue(value);
        EnsureItemValueAvailable(normalizedValue, itemId);
        item.Update(normalizedValue, label, sortOrder);
        MarkChanged(updatedBy, updatedAt);
    }

    /// <summary>
    /// 移除指定的数据字典项。
    /// </summary>
    /// <param name="itemId">数据字典项 ID。</param>
    /// <param name="version">用于并发校验的当前版本。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    public void RemoveItem(Guid itemId, Guid version, Guid updatedBy, DateTimeOffset updatedAt)
    {
        ValidateMutation(version, updatedAt);
        var item = GetRequiredItem(itemId);
        _items.Remove(item);
        MarkChanged(updatedBy, updatedAt);
    }

    /// <summary>
    /// 获取指定的数据字典项。
    /// </summary>
    /// <param name="itemId">数据字典项 ID。</param>
    /// <returns>匹配的数据字典项。</returns>
    private DataDictionaryItem GetRequiredItem(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.ItemIdRequired);
        }

        return _items.SingleOrDefault(item => item.Id == itemId)
            ?? throw new DataDictionaryDomainException(
                MessageKeys.DataDictionaries.ItemNotFound,
                DomainErrorType.NotFound
            );
    }

    /// <summary>
    /// 确保数据字典项值在当前字典内唯一。
    /// </summary>
    /// <param name="value">待校验的数据字典项值。</param>
    /// <param name="excludedId">不参与校验的数据字典项 ID。</param>
    private void EnsureItemValueAvailable(string value, Guid? excludedId)
    {
        if (_items.Any(item => item.Id != excludedId && string.Equals(item.Value, value, StringComparison.Ordinal)))
        {
            throw new DataDictionaryDomainException(
                MessageKeys.DataDictionaries.ItemValueExists,
                DomainErrorType.Conflict
            );
        }
    }

    /// <summary>
    /// 校验数据字典变更的版本和修改时间。
    /// </summary>
    /// <param name="version">用于并发校验的当前版本。</param>
    /// <param name="updatedAt">修改时间。</param>
    private void ValidateMutation(Guid version, DateTimeOffset updatedAt)
    {
        if (version != Version)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.DataChanged, DomainErrorType.Conflict);
        }

        if (updatedAt == default)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.ModifiedAtRequired);
        }
    }

    /// <summary>
    /// 记录数据字典的修改信息。
    /// </summary>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    private void MarkChanged(Guid updatedBy, DateTimeOffset updatedAt)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

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

    /// <summary>
    /// 规范化可选文本。
    /// </summary>
    /// <param name="value">待处理的文本。</param>
    /// <returns>去除首尾空白后的文本；空白文本返回 <see langword="null" />。</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
