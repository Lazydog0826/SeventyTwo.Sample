namespace SeventyTwo.Sample.WebApi.Contracts.Organizations;

/// <summary>
/// 机构创建请求。
/// </summary>
/// <param name="Code">机构编码。</param>
/// <param name="Name">机构名称。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="ParentId">上级机构 ID；为空时创建根机构。</param>
/// <param name="SortOrder">排序号。</param>
public record CreateOrganizationRequest(string Code, string Name, bool Enable, Guid? ParentId, int SortOrder = 0);

/// <summary>
/// 机构修改请求。
/// </summary>
/// <param name="Id">机构 ID。</param>
/// <param name="Code">机构编码。</param>
/// <param name="Name">机构名称。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="ParentId">上级机构 ID。</param>
/// <param name="Version">客户端持有的并发版本。</param>
/// <param name="SortOrder">排序号。</param>
public sealed record UpdateOrganizationRequest(
    Guid Id,
    string Code,
    string Name,
    bool Enable,
    Guid? ParentId,
    Guid Version,
    int SortOrder = 0
) : CreateOrganizationRequest(Code, Name, Enable, ParentId, SortOrder);

/// <summary>
/// 机构删除请求。
/// </summary>
/// <param name="Id">机构 ID。</param>
public sealed record DeleteOrganizationRequest(Guid Id);
