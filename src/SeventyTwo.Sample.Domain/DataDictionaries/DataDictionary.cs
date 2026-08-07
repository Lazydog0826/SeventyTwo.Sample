// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.DataDictionaries;

public sealed class DataDictionary : AggregateRoot
{
    private DataDictionary() { }

    public DataDictionary(Guid id, string code, string name, string? description = null)
    {
        if (id == Guid.Empty)
        {
            throw new DataDictionaryDomainException("数据字典 ID 不能为空");
        }

        Id = id;
        Code = RequireText(code, "数据字典编码不能为空");
        Name = RequireText(name, "数据字典名称不能为空");
        Description = NormalizeOptional(description);
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    private static string RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DataDictionaryDomainException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
