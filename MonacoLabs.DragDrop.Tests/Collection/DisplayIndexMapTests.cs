using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DisplayIndexMapTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(5, 5)]
    public void AViewOfTheWholeCollectionMapsEachIndexToItself(int displayIndex, int expected) =>
        Assert.Equal(expected, Map("a,b,c,d,e", "a,b,c,d,e", displayIndex));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    public void AShownItemMapsToItsIndexInTheCollection(int displayIndex, int expected) =>
        Assert.Equal(expected, Map("a,b,c,d,e", "b,d", displayIndex));

    [Fact]
    public void ThePositionAfterTheLastShownItemMapsJustAfterItInTheCollection() =>
        Assert.Equal(4, Map("a,b,c,d,e", "b,d", 2));

    [Fact]
    public void AnEmptyViewMapsToNull() =>
        Assert.Null(Map("a,b,c", "", 0));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void AShownItemTheCollectionDoesNotContainMapsToNull(int displayIndex) =>
        Assert.Null(Map("a,b,c", "b,x", displayIndex));

    [Theory]
    [InlineData("x,x,c,x", 0, 1)]
    [InlineData("x,x,c,x", 1, 3)]
    [InlineData("x,x,c,x", 2, 4)]
    [InlineData("x,x,c,x", 3, 5)]
    [InlineData("c,x,x,x", 1, 1)]
    [InlineData("c,x,x,x", 3, 5)]
    public void EqualItemsAreMatchedByOccurrence(string view, int displayIndex, int expected) =>
        Assert.Equal(expected, Map("a,x,b,x,c,x", view, displayIndex));

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void AnIndexOutsideTheViewMapsToNull(int displayIndex) =>
        Assert.Null(Map("a,b,c", "a,b", displayIndex));

    private static int? Map(string collection, string view, int displayIndex) =>
        DisplayIndexMap.ToCollectionIndex(Split(view), Split(collection).ToList(), displayIndex);

    private static object?[] Split(string items) => items.Length == 0 ? [] : items.Split(',');
}
