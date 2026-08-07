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
}

[SugarTable("organization_member")]
[SugarIndex(
    "uq_organization_member_organization_user",
    nameof(OrganizationId),
    OrderByType.Asc,
    nameof(UserId),
    OrderByType.Asc,
    true
)]
[SugarIndex("ix_organization_member_user_id", nameof(UserId), OrderByType.Asc)]
internal sealed class OrganizationMemberRecord : BaseEntity
{
    /// <summary>
    /// 所属机构 ID。
    /// </summary>
    [SugarColumn(ColumnName = "organization_id", ColumnDataType = "uuid")]
    public Guid OrganizationId { get; init; }

    /// <summary>
    /// 用户 ID。
    /// </summary>
    [SugarColumn(ColumnName = "user_id", ColumnDataType = "uuid")]
    public Guid UserId { get; init; }

    /// <summary>
    /// 是否为用户的主机构。
    /// </summary>
    [SugarColumn(ColumnName = "is_primary")]
    public bool IsPrimary { get; init; }
}
