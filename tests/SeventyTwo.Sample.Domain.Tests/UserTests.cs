using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Create_ShouldNormalizeRequiredProfileFields()
    {
        var user = new User(
            Guid.CreateVersion7(),
            " user ",
            "hash",
            " 张三 ",
            " 13800000000 ",
            " a@example.com ",
            DataPermissionType.Self
        );

        Assert.Equal("user", user.Username);
        Assert.Equal("张三", user.DisplayName);
        Assert.Equal("13800000000", user.Phone);
        Assert.Equal("a@example.com", user.Email);
    }

    [Theory]
    [InlineData("", "13800000000", "a@example.com", MessageKeys.Users.DisplayNameRequired)]
    [InlineData("张三", "", "a@example.com", MessageKeys.Users.PhoneRequired)]
    [InlineData("张三", "13800000000", "", MessageKeys.Users.EmailRequired)]
    public void Create_WithMissingProfileField_ShouldFail(string name, string phone, string email, string message)
    {
        var exception = Assert.Throws<UserDomainException>(() =>
            new User(Guid.CreateVersion7(), "user", "hash", name, phone, email, DataPermissionType.Self)
        );
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Create_WithSuperAdminUsername_ShouldFail()
    {
        var exception = Assert.Throws<UserDomainException>(() =>
            CreateUser(SystemUsernames.SuperAdmin)
        );

        Assert.Equal(MessageKeys.Users.UsernameReserved, exception.Message);
    }

    [Fact]
    public void Create_WithDataPermissionType_ShouldSetDataPermissionType()
    {
        var user = new User(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.OrganizationAndDescendants
        );

        Assert.Equal(DataPermissionType.OrganizationAndDescendants, user.DataPermissionType);
    }

    [Fact]
    public void Create_WithInvalidDataPermissionType_ShouldFail()
    {
        var exception = Assert.Throws<UserDomainException>(() =>
            new User(
                Guid.CreateVersion7(),
                "user",
                "hash",
                "测试用户",
                "13800000000",
                "user@example.com",
                (DataPermissionType)short.MaxValue
            )
        );

        Assert.Equal(MessageKeys.Users.DataPermissionTypeInvalid, exception.Message);
    }

    [Fact]
    public void UpdateProfile_WithStaleVersion_ShouldFail()
    {
        var user = CreateUser("user");
        var exception = Assert.Throws<UserDomainException>(() =>
            user.UpdateProfile("新姓名", "13900000000", "new@example.com", Guid.CreateVersion7(), Guid.Empty, DateTimeOffset.UtcNow)
        );
        Assert.Equal(MessageKeys.Users.DataChanged, exception.Message);
    }

    [Fact]
    public void UpdateProfile_ShouldSetAndClearDefaultPage()
    {
        var user = CreateUser("user");
        var pageId = Guid.CreateVersion7();

        user.UpdateProfile(
            "新姓名",
            "13900000000",
            "new@example.com",
            pageId,
            user.Version,
            Guid.Empty,
            DateTimeOffset.UtcNow
        );
        Assert.Equal(pageId, user.DefaultPageId);

        user.UpdateProfile(
            "新姓名",
            "13900000000",
            "new@example.com",
            Guid.Empty,
            user.Version,
            Guid.Empty,
            DateTimeOffset.UtcNow
        );
        Assert.Null(user.DefaultPageId);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("enable")]
    [InlineData("delete")]
    public void SuperAdmin_ShouldRejectAllManagementMutations(string operation)
    {
        var user = User.Restore(
            Guid.CreateVersion7(),
            SystemUsernames.SuperAdmin,
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.All
        );
        user.Version = Guid.CreateVersion7();

        var exception = Assert.Throws<UserDomainException>(() =>
        {
            if (operation == "update") user.UpdateProfile("管理员", "13900000000", "new@example.com", user.Version, Guid.Empty, DateTimeOffset.UtcNow);
            else if (operation == "enable") user.SetEnable(false, user.Version, Guid.Empty, DateTimeOffset.UtcNow);
            else user.EnsureCanDelete(user.Version);
        });

        Assert.Equal(MessageKeys.Users.SuperAdminProtected, exception.Message);
    }

    private static User CreateUser(string username) =>
        new(
            Guid.CreateVersion7(),
            username,
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self
        )
        {
            Version = Guid.CreateVersion7(),
        };
}
