using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain.DataDictionaries;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class DataDictionaryTests
{
    [Fact]
    public void Constructor_ShouldNormalizeText()
    {
        var dictionary = new DataDictionary(Guid.CreateVersion7(), " CODE ", " 名称 ", " 说明 ");

        Assert.Equal("CODE", dictionary.Code);
        Assert.Equal("名称", dictionary.Name);
        Assert.Equal("说明", dictionary.Description);
    }

    [Fact]
    public void AddItem_ShouldAddNormalizedItemAndSetAuditFields()
    {
        var dictionary = CreateDictionary();
        var itemId = Guid.CreateVersion7();
        var updatedBy = Guid.CreateVersion7();
        var updatedAt = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

        dictionary.AddItem(itemId, " 1 ", " 启用 ", 1, dictionary.Version, updatedBy, updatedAt);

        var item = Assert.Single(dictionary.Items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal("1", item.Value);
        Assert.Equal("启用", item.Label);
        Assert.Equal(1, item.SortOrder);
        Assert.Equal(updatedBy, dictionary.UpdatedBy);
        Assert.Equal(updatedAt, dictionary.UpdatedAt);
    }

    [Fact]
    public void UpdateItem_ShouldUpdateRequestedItem()
    {
        var dictionary = CreateDictionary();
        var itemId = Guid.CreateVersion7();
        var updatedAt = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);
        dictionary.AddItem(itemId, "1", "启用", 1, dictionary.Version, Guid.Empty, updatedAt);

        dictionary.UpdateItem(itemId, " 2 ", " 禁用 ", 2, dictionary.Version, Guid.Empty, updatedAt);

        var item = Assert.Single(dictionary.Items);
        Assert.Equal("2", item.Value);
        Assert.Equal("禁用", item.Label);
        Assert.Equal(2, item.SortOrder);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveRequestedItem()
    {
        var dictionary = CreateDictionary();
        var itemId = Guid.CreateVersion7();
        var updatedAt = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);
        dictionary.AddItem(itemId, "1", "启用", 1, dictionary.Version, Guid.Empty, updatedAt);

        dictionary.RemoveItem(itemId, dictionary.Version, Guid.Empty, updatedAt);

        Assert.Empty(dictionary.Items);
    }

    [Fact]
    public void AddItem_WithDuplicateValue_ShouldFail()
    {
        var dictionary = CreateDictionary();
        dictionary.AddItem(Guid.CreateVersion7(), "A", "甲", 0, dictionary.Version, Guid.Empty, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DataDictionaryDomainException>(() =>
            dictionary.AddItem(
                Guid.CreateVersion7(),
                "A",
                "乙",
                1,
                dictionary.Version,
                Guid.Empty,
                DateTimeOffset.UtcNow
            )
        );

        Assert.Equal(MessageKeys.DataDictionaries.ItemValueExists, exception.Message);
    }

    [Fact]
    public void Update_WithStaleVersion_ShouldFail()
    {
        var dictionary = CreateDictionary();

        var exception = Assert.Throws<DataDictionaryDomainException>(() =>
            dictionary.Update("CODE", "名称", null, true, Guid.CreateVersion7(), Guid.Empty, DateTimeOffset.UtcNow)
        );

        Assert.Equal(MessageKeys.DataDictionaries.DataChanged, exception.Message);
    }

    [Fact]
    public void AddItem_WithNegativeSortOrder_ShouldFail()
    {
        var dictionary = CreateDictionary();

        var exception = Assert.Throws<DataDictionaryDomainException>(() =>
            dictionary.AddItem(
                Guid.CreateVersion7(),
                "A",
                "甲",
                -1,
                dictionary.Version,
                Guid.Empty,
                DateTimeOffset.UtcNow
            )
        );

        Assert.Equal(MessageKeys.DataDictionaries.ItemSortMustNotBeNegative, exception.Message);
    }

    private static DataDictionary CreateDictionary() =>
        new(Guid.CreateVersion7(), "CODE", "名称") { Version = Guid.CreateVersion7() };
}
