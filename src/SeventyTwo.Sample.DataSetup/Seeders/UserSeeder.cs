using Microsoft.AspNetCore.Identity;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.Infrastructure.Users;
using SqlSugar;

namespace SeventyTwo.Sample.DataSetup.Seeders;

// 用户种子：超级管理员与测试用户，并为测试用户分配首页权限。
internal static class UserSeeder
{
    // 项目为 DEMO 账号密码写死即可
    private const string SuperAdminDisplayName = "超级管理员";
    private const string InitialPassword = "123456";
    private const string SuperAdminPhone = "00000000000";
    private const string SuperAdminEmail = "superadmin@localhost";

    public static void Seed(SqlSugarClient db, Guid homePermissionId, OrganizationSeedResult organizations)
    {
        var superAdminUserId = Guid.CreateVersion7();
        var passwordHash = new PasswordHasher<string>().HashPassword(SystemUsernames.SuperAdmin, InitialPassword);

        db.Insertable(
                new UserAccountRecord
                {
                    Id = superAdminUserId,
                    Username = SystemUsernames.SuperAdmin,
                    PasswordHash = passwordHash,
                    DisplayName = SuperAdminDisplayName,
                    Phone = SuperAdminPhone,
                    Email = SuperAdminEmail,
                    DataPermissionType = DataPermissionType.All,
                    DefaultPageId = homePermissionId,
                    OrgId = Guid.Empty,
                }
            )
            .ExecuteCommand();

        // 测试用户分布在不同机构层级，并覆盖全部数据权限类型。
        var testUsers = new[]
        {
            CreateUser(
                "group.admin",
                "集团管理员",
                "13800000001",
                "group.admin@example.com",
                DataPermissionType.All,
                organizations.GroupId,
                homePermissionId,
                InitialPassword
            ),
            CreateUser(
                "east.manager",
                "华东区域经理",
                "13800000002",
                "east.manager@example.com",
                DataPermissionType.OrganizationAndDescendants,
                organizations.EastRegionId,
                homePermissionId,
                InitialPassword
            ),
            CreateUser(
                "shanghai.manager",
                "上海分公司经理",
                "13800000003",
                "shanghai.manager@example.com",
                DataPermissionType.Organization,
                organizations.ShanghaiBranchId,
                homePermissionId,
                InitialPassword
            ),
            CreateUser(
                "pudong.sales",
                "浦东销售专员",
                "13800000004",
                "pudong.sales@example.com",
                DataPermissionType.Self,
                organizations.PudongSalesId,
                homePermissionId,
                InitialPassword
            ),
            CreateUser(
                "shenzhen.manager",
                "深圳分公司经理",
                "13800000005",
                "shenzhen.manager@example.com",
                DataPermissionType.Organization,
                organizations.ShenzhenBranchId,
                homePermissionId,
                InitialPassword
            ),
            CreateUser(
                "operations.manager",
                "运营中心经理",
                "13800000006",
                "operations.manager@example.com",
                DataPermissionType.OrganizationAndDescendants,
                organizations.OperationsCenterId,
                homePermissionId,
                InitialPassword
            ),
        };
        db.Insertable(testUsers).ExecuteCommand();

        db.Insertable(
                testUsers
                    .Select(user => new UserPermissionRecord
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = user.Id,
                        PermissionId = homePermissionId,
                        OrgId = Guid.Empty,
                    })
                    .ToArray()
            )
            .ExecuteCommand();
    }

    private static UserAccountRecord CreateUser(
        string username,
        string displayName,
        string phone,
        string email,
        DataPermissionType dataPermissionType,
        Guid orgId,
        Guid defaultPageId,
        string password
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            PasswordHash = new PasswordHasher<string>().HashPassword(username, password),
            DisplayName = displayName,
            Phone = phone,
            Email = email,
            DataPermissionType = dataPermissionType,
            DefaultPageId = defaultPageId,
            OrgId = orgId,
        };
}
