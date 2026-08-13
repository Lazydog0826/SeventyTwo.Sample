using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class PermissionTests
{
    [Fact]
    public void Constructor_ShouldBuildHierarchyPath()
    {
        var root = CreatePage();
        var childId = Guid.CreateVersion7();
        var child = new Permission(
            childId,
            "child",
            "子权限",
            PermissionType.Page,
            0,
            string.Empty,
            "/src/views/child.vue",
            "/child",
            "Child",
            root.Id,
            new PermissionMetaData(true),
            $"{root.Path}/{childId}"
        );

        Assert.Equal(root.Id.ToString(), root.Path);
        Assert.Equal($"{root.Id}/{childId}", child.Path);
    }

    [Fact]
    public void ChangePath_ShouldUseNewParentPath()
    {
        var permission = CreatePage();
        var parentId = Guid.CreateVersion7();

        permission.ChangePath(parentId.ToString());

        Assert.Equal($"{parentId}/{permission.Id}", permission.Path);
    }

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
    [InlineData("", MessageKeys.Permissions.VueComponentPathRequired)]
    [InlineData("route", MessageKeys.Permissions.RoutePathRequired)]
    [InlineData("name", MessageKeys.Permissions.RouteNameRequired)]
    public void CreatePage_WithMissingRouteInformation_ShouldFail(string missingField, string expectedMessage)
    {
        var vueComponentPath = missingField.Length == 0 ? string.Empty : "Page.vue";
        var routePath = missingField == "route" ? string.Empty : "/page";
        var routeName = missingField == "name" ? string.Empty : "Page";

        var exception = Assert.Throws<PermissionDomainException>(() =>
            new Permission(
                Guid.CreateVersion7(),
                "page",
                "页面",
                PermissionType.Page,
                0,
                string.Empty,
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
            new Permission(
                Guid.CreateVersion7(),
                "button",
                "按钮",
                PermissionType.Button,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                null
            )
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
                string.Empty,
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
            string.Empty,
            " Page.vue ",
            " /page ",
            " Page ",
            null,
            new PermissionMetaData(true)
        );
}
