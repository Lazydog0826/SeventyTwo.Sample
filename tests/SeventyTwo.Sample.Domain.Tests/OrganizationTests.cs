using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Organizations;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class OrganizationTests
{
    [Fact]
    public void Create_ShouldNormalizeValues()
    {
        var organization = new Organization(Guid.CreateVersion7(), " code ", " name ");

        Assert.Equal("code", organization.Code);
        Assert.Equal("name", organization.Name);
        Assert.Null(organization.ParentId);
        Assert.Equal(organization.Id.ToString(), organization.Path);
    }

    [Fact]
    public void ChangePath_ShouldAppendOrganizationIdToParentPath()
    {
        var rootId = Guid.CreateVersion7();
        var childId = Guid.CreateVersion7();
        var child = new Organization(childId, "child", "子机构", rootId, rootId.ToString());

        child.ChangePath(rootId.ToString());

        Assert.Equal($"{rootId}/{child.Id}", child.Path);
    }

    [Fact]
    public void Create_WithSelfAsParent_ShouldFail()
    {
        var id = Guid.CreateVersion7();

        var exception = Assert.Throws<OrganizationDomainException>(() =>
            new Organization(id, "code", "名称", id, id.ToString())
        );

        Assert.Equal(MessageKeys.Organizations.SelfCannotBeParent, exception.Message);
    }

    [Theory]
    [InlineData("", "name", "organization.codeRequired")]
    [InlineData("code", " ", "organization.nameRequired")]
    public void Create_WithRequiredValueMissing_ShouldFail(string code, string name, string message)
    {
        var exception = Assert.Throws<OrganizationDomainException>(() =>
            new Organization(Guid.CreateVersion7(), code, name)
        );

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Update_ShouldChangeValuesAndAuditFields()
    {
        var organization = new Organization(Guid.CreateVersion7(), "old", "旧名称") { Version = Guid.CreateVersion7() };
        var updatedAt = DateTimeOffset.UtcNow;

        organization.Update(" new ", " 新名称 ", false, null, organization.Version, SystemIds.System, updatedAt, 2);

        Assert.Equal("new", organization.Code);
        Assert.Equal("新名称", organization.Name);
        Assert.False(organization.Enable);
        Assert.Equal(2, organization.SortOrder);
        Assert.Equal(updatedAt, organization.UpdatedAt);
    }

    [Fact]
    public void Update_WithStaleVersion_ShouldFail()
    {
        var organization = new Organization(Guid.CreateVersion7(), "code", "名称") { Version = Guid.CreateVersion7() };

        var exception = Assert.Throws<OrganizationDomainException>(() =>
            organization.Update(
                "code",
                "名称",
                true,
                null,
                Guid.CreateVersion7(),
                SystemIds.System,
                DateTimeOffset.UtcNow
            )
        );

        Assert.Equal(MessageKeys.Organizations.DataChanged, exception.Message);
    }

    [Fact]
    public void Update_WithMissingUpdatedAt_ShouldNotChangeOrganization()
    {
        var organization = new Organization(Guid.CreateVersion7(), "old", "旧名称") { Version = Guid.CreateVersion7() };

        var exception = Assert.Throws<OrganizationDomainException>(() =>
            organization.Update("new", "新名称", false, null, organization.Version, SystemIds.System, default)
        );

        Assert.Equal(MessageKeys.Organizations.ModifiedAtRequired, exception.Message);
        Assert.Equal("old", organization.Code);
        Assert.Equal("旧名称", organization.Name);
        Assert.True(organization.Enable);
    }

    [Fact]
    public void Create_WithNegativeSortOrder_ShouldFail()
    {
        var exception = Assert.Throws<OrganizationDomainException>(() =>
            new Organization(Guid.CreateVersion7(), "code", "名称", sortOrder: -1)
        );

        Assert.Equal(MessageKeys.Organizations.SortMustNotBeNegative, exception.Message);
    }
}
