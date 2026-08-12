// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Application.Organizations;

/// <summary>
/// 机构应用服务。
/// </summary>
public interface IOrganizationApplication
{
    /// <summary>
    /// 创建机构。
    /// </summary>
    /// <param name="input">创建机构的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的机构信息。</returns>
    Task<OrganizationListOutput> CreateAsync(CreateOrganizationInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 更新指定机构。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="input">更新机构的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(Guid id, UpdateOrganizationInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 删除指定机构。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取机构列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>机构只读列表。</returns>
    Task<IReadOnlyList<OrganizationListOutput>> GetListAsync(CancellationToken cancellationToken);
}

/// <summary>
/// 机构列表项。
/// </summary>
public sealed record OrganizationListOutput
{
    /// <summary>
    /// 机构 ID。
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 机构编码。
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 机构名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enable { get; init; }

    /// <summary>
    /// 上级机构 ID；根机构为 <see langword="null"/>。
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// 并发版本。
    /// </summary>
    public Guid Version { get; init; }
}

/// <summary>
/// 创建机构的输入。
/// </summary>
/// <param name="Code">机构编码。</param>
/// <param name="Name">机构名称。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="ParentId">上级机构 ID；根机构为 <see langword="null"/>。</param>
/// <param name="SortOrder">排序号。</param>
public record CreateOrganizationInput(string Code, string Name, bool Enable, Guid? ParentId, int SortOrder = 0);

/// <summary>
/// 更新机构的输入。
/// </summary>
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
