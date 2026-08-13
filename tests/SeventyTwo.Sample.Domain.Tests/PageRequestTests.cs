namespace SeventyTwo.Sample.Domain.Tests;

public sealed class PageRequestTests
{
    [Theory]
    [InlineData(2_147_484, 1_000, true)]
    [InlineData(2_147_485, 1_000, false)]
    [InlineData(int.MaxValue, 1, true)]
    public void IsOffsetWithinRange_ShouldCheckCombinedPaginationOffset(int index, int limit, bool expected)
    {
        var request = new PageRequest { Index = index, Limit = limit };

        Assert.Equal(expected, request.IsOffsetWithinRange());
    }
}
