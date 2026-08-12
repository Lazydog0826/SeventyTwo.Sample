using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.Organizations;

[SugarTable("organization")]
[SugarIndex("uq_organization_org_code", nameof(OrgId), OrderByType.Asc, nameof(Code), OrderByType.Asc, true)]
[SugarIndex("ix_organization_parent_id", nameof(ParentId), OrderByType.Asc)]
internal sealed class OrganizationRecord : BaseEntity
{
    /// <summary>
    /// 机构编码。
    /// </summary>
    [SugarColumn(ColumnName = "code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 机构名称。
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 上级机构 ID。
    /// </summary>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true, ColumnDataType = "uuid")]
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 由机构 ID 组成的完整层级路径。
    /// </summary>
    [SugarColumn(ColumnName = "path", ColumnDataType = "text")]
    public string Path { get; init; } = string.Empty;
}
