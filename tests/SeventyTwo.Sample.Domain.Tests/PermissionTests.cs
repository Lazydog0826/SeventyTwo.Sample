using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class PermissionTests
{
    [Fact]
    public void CreatePage_ShouldNormalizeAndRequireRouteInformation()
    {
        var permission = CreatePage();

        Assert.Equal("page", permission.Code);
        Assert.Equal("页面", permission.Title);
        Assert.Equal("Page.vue", permission.VueComponentPath);
        Assert.Equal("/page", permission.RoutePath);
        Assert.Equal("Page", permission.RouteName);
        Assert.Equal(new PermissionMetaData(true), permission.MetaData);
    }

    [Theory]
    [InlineData(null, MessageKeys.Permissions.VueComponentPathRequired)]
    [InlineData("route", MessageKeys.Permissions.RoutePathRequired)]
    [InlineData("name", MessageKeys.Permissions.RouteNameRequired)]
    public void CreatePage_WithMissingRouteInformation_ShouldFail(string? missingField, string expectedMessage)
    {
        var vueComponentPath = missingField == null ? null : "Page.vue";
        var routePath = missingField == "route" ? null : "/page";
        var routeName = missingField == "name" ? null : "Page";

        var exception = Assert.Throws<PermissionDomainException>(() =>
            new Permission(
                Guid.CreateVersion7(),
                "page",
                "页面",
                PermissionType.Page,
                0,
                null,
                vueComponentPath,
                routePath,
                routeName,
                null,
                new PermissionMetaData(true)
            )
        );

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void CreateButton_WithoutParent_ShouldFail()
    {
        var exception = Assert.Throws<PermissionDomainException>(() =>
            new Permission(Guid.CreateVersion7(), "button", "按钮", PermissionType.Button, 0, null, null, null, null, null)
        );

        Assert.Equal(MessageKeys.Permissions.ButtonParentRequired, exception.Message);
    }

    [Fact]
    public void Update_WithInvalidInfo_ShouldNotChangePermission()
    {
        var permission = CreatePage();

        var exception = Assert.Throws<PermissionDomainException>(() =>
            permission.Update(
                "changed",
                "新标题",
                PermissionType.Page,
                false,
                -1,
                null,
                "Changed.vue",
                "/changed",
                "Changed",
                null,
                new PermissionMetaData(false),
                permission.Version,
                SystemIds.System,
                new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero)
            )
        );

        Assert.Equal(MessageKeys.Permissions.SortMustNotBeNegative, exception.Message);
        Assert.Equal("page", permission.Code);
        Assert.Equal("页面", permission.Title);
        Assert.True(permission.Enable);
        Assert.Null(permission.UpdatedAt);
    }

    private static Permission CreatePage() =>
        new(
            Guid.CreateVersion7(),
            " page ",
            " 页面 ",
            PermissionType.Page,
            0,
            null,
            " Page.vue ",
            " /page ",
            " Page ",
            null,
            new PermissionMetaData(true)
        );
}
