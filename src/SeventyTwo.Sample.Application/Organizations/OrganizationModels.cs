// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Application.Organizations;

/// <summary>机构列表项。</summary>
public sealed record OrganizationListOutput
{
    /// <summary>机构 ID。</summary>
    public Guid Id { get; init; }

    /// <summary>机构编码。</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>机构名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>是否启用。</summary>
    public bool Enable { get; init; }

    /// <summary>上级机构 ID；根机构为 <see langword="null"/>。</summary>
    public Guid? ParentId { get; init; }

    /// <summary>排序号。</summary>
    public int SortOrder { get; init; }

    /// <summary>并发版本。</summary>
    public Guid Version { get; init; }
}

/// <summary>创建机构的输入。</summary>
/// <param name="Code">机构编码。</param>
/// <param name="Name">机构名称。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="ParentId">上级机构 ID；根机构为 <see langword="null"/>。</param>
/// <param name="SortOrder">排序号。</param>
public record CreateOrganizationInput(string Code, string Name, bool Enable, Guid? ParentId, int SortOrder = 0);

/// <summary>更新机构的输入。</summary>
/// <param name="Code">机构编码。</param>
/// <param name="Name">机构名称。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="ParentId">上级机构 ID；根机构为 <see langword="null"/>。</param>
/// <param name="SortOrder">排序号。</param>
/// <param name="Version">客户端读取机构时获得的并发版本。</param>
public sealed record UpdateOrganizationInput(
    string Code,
    string Name,
    bool Enable,
    Guid? ParentId,
    Guid Version,
    int SortOrder = 0
) : CreateOrganizationInput(Code, Name, Enable, ParentId, SortOrder);
