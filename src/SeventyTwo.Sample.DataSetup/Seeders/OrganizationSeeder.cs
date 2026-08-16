using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Infrastructure.Organizations;
using SqlSugar;

namespace SeventyTwo.Sample.DataSetup.Seeders;

// 机构种子结果：仅暴露被用户种子引用的机构 Id，其余机构无下游引用。
internal sealed record OrganizationSeedResult(
    Guid GroupId,
    Guid EastRegionId,
    Guid ShanghaiBranchId,
    Guid PudongSalesId,
    Guid ShenzhenBranchId,
    Guid OperationsCenterId
);

// 机构种子：构建测试机构树。
internal static class OrganizationSeeder
{
    public static OrganizationSeedResult Seed(SqlSugarClient db)
    {
        var groupId = Guid.CreateVersion7();
        var eastRegionId = Guid.CreateVersion7();
        var shanghaiBranchId = Guid.CreateVersion7();
        var pudongSalesId = Guid.CreateVersion7();
        var xuhuiResearchId = Guid.CreateVersion7();
        var hangzhouBranchId = Guid.CreateVersion7();
        var southRegionId = Guid.CreateVersion7();
        var shenzhenBranchId = Guid.CreateVersion7();
        var partnerCompanyId = Guid.CreateVersion7();
        var operationsCenterId = Guid.CreateVersion7();

        // 测试机构包含两棵机构树，主机构树最深四级，便于验证机构树和下级数据权限。
        var group = CreateOrganization(groupId, "GROUP", "示例集团", null, null, groupId, 10);
        var eastRegion = CreateOrganization(eastRegionId, "EAST", "华东区域", groupId, group.Path, groupId, 20);
        var shanghaiBranch = CreateOrganization(
            shanghaiBranchId,
            "SHANGHAI",
            "上海分公司",
            eastRegionId,
            eastRegion.Path,
            groupId,
            30
        );
        var pudongSales = CreateOrganization(
            pudongSalesId,
            "PUDONG_SALES",
            "浦东销售部",
            shanghaiBranchId,
            shanghaiBranch.Path,
            groupId,
            40
        );
        var xuhuiResearch = CreateOrganization(
            xuhuiResearchId,
            "XUHUI_RESEARCH",
            "徐汇研发部",
            shanghaiBranchId,
            shanghaiBranch.Path,
            groupId,
            50
        );
        var hangzhouBranch = CreateOrganization(
            hangzhouBranchId,
            "HANGZHOU",
            "杭州分公司",
            eastRegionId,
            eastRegion.Path,
            groupId,
            60
        );
        var southRegion = CreateOrganization(southRegionId, "SOUTH", "华南区域", groupId, group.Path, groupId, 70);
        var shenzhenBranch = CreateOrganization(
            shenzhenBranchId,
            "SHENZHEN",
            "深圳分公司",
            southRegionId,
            southRegion.Path,
            groupId,
            80
        );
        var partnerCompany = CreateOrganization(
            partnerCompanyId,
            "PARTNER",
            "合作伙伴公司",
            null,
            null,
            partnerCompanyId,
            90
        );
        var operationsCenter = CreateOrganization(
            operationsCenterId,
            "OPERATIONS",
            "运营中心",
            partnerCompanyId,
            partnerCompany.Path,
            partnerCompanyId,
            100
        );
        db.Insertable(
                new[]
                {
                    group,
                    eastRegion,
                    shanghaiBranch,
                    pudongSales,
                    xuhuiResearch,
                    hangzhouBranch,
                    southRegion,
                    shenzhenBranch,
                    partnerCompany,
                    operationsCenter,
                }
            )
            .ExecuteCommand();

        return new OrganizationSeedResult(
            groupId,
            eastRegionId,
            shanghaiBranchId,
            pudongSalesId,
            shenzhenBranchId,
            operationsCenterId
        );
    }

    private static OrganizationRecord CreateOrganization(
        Guid id,
        string code,
        string name,
        Guid? parentId,
        string? parentPath,
        Guid orgId,
        int sortOrder
    ) =>
        new()
        {
            Id = id,
            Code = code,
            Name = name,
            ParentId = parentId,
            Path = parentPath is null ? id.ToString() : $"{parentPath}/{id}",
            SortOrder = sortOrder,
            OrgId = orgId,
            Enable = true,
            CreatedBy = SystemIds.System,
            CreatedAt = DateTimeExtension.Now(),
            Version = Guid.CreateVersion7(),
        };
}
