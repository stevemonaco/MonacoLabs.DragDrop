using System.Collections;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class CollectionRemovalTests
{
    [Fact]
    public void AMoveToAnotherListRemovesThePressedIndex()
    {
        var source = new List<string> { "a", "b", "c" };
        var target = new List<string> { "x" };
        Drop(target, "b", 0);

        CollectionRemoval.RemoveAfterDrop(source, "b", pressedIndex: 1, target, targetIndex: 0);

        Assert.Equal(["a", "c"], source);
        Assert.Equal(["b", "x"], target);
    }

    [Theory]
    [InlineData(0, 3, new[] { "b", "c", "a" })]
    [InlineData(0, 2, new[] { "b", "a", "c" })]
    [InlineData(2, 0, new[] { "c", "a", "b" })]
    [InlineData(2, 1, new[] { "a", "c", "b" })]
    [InlineData(1, 1, new[] { "a", "b", "c" })]
    [InlineData(1, 2, new[] { "a", "b", "c" })]
    public void AMoveWithinOneListRemovesTheOriginalAfterTheInsertionShiftsIt(int pressedIndex, int targetIndex, string[] expected)
    {
        var list = new List<string> { "a", "b", "c" };
        var payload = list[pressedIndex];
        Drop(list, payload, targetIndex);

        CollectionRemoval.RemoveAfterDrop(list, payload, pressedIndex, list, targetIndex);

        Assert.Equal(expected, list);
    }

    [Fact]
    public void AnAppendWithinOneListRemovesThePressedIndex()
    {
        var list = new List<string> { "a", "b", "c" };
        list.Add("a");

        CollectionRemoval.RemoveAfterDrop(list, "a", pressedIndex: 0, list, targetIndex: null);

        Assert.Equal(["b", "c", "a"], list);
    }

    [Fact]
    public void WithoutAPressedIndexTheMoveRemovesThePayloadByEquality()
    {
        var source = new List<string> { "a", "b", "c" };
        var target = new List<string>();
        Drop(target, "b", 0);

        CollectionRemoval.RemoveAfterDrop(source, "b", pressedIndex: null, target, targetIndex: 0);

        Assert.Equal(["a", "c"], source);
    }

    [Fact]
    public void AStalePressedIndexFallsBackToTheFirstEqualItem()
    {
        var source = new List<string> { "a", "b", "c" };
        var target = new List<string>();
        Drop(target, "c", 0);

        CollectionRemoval.RemoveAfterDrop(source, "c", pressedIndex: 0, target, targetIndex: 0);

        Assert.Equal(["a", "b"], source);
    }

    [Fact]
    public void TheEqualitySearchSkipsTheItemTheDropInserted()
    {
        var list = new List<string> { "y", "x" };
        Drop(list, "x", 0);

        CollectionRemoval.RemoveAfterDrop(list, "x", pressedIndex: null, list, targetIndex: 0);

        Assert.Equal(["x", "y"], list);
    }

    [Fact]
    public void MovingOneOfTwoEqualItemsRemovesThatOne()
    {
        var list = new List<string> { "x", "y", "x" };
        Drop(list, "x", 1);

        CollectionRemoval.RemoveAfterDrop(list, "x", pressedIndex: 2, list, targetIndex: 1);

        Assert.Equal(["x", "x", "y"], list);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(null)]
    public void WithoutAPressedIndexAnAppendRemovesTheOriginalNotTheAppendedItem(int? targetIndex)
    {
        var list = new List<string> { "a", "b" };
        list.Add("a");

        CollectionRemoval.RemoveAfterDrop(list, "a", pressedIndex: null, list, targetIndex);

        Assert.Equal(["b", "a"], list);
    }

    [Fact]
    public void AMoveOntoATargetWithoutACollectionRemovesThePressedIndex()
    {
        var source = new List<string> { "a", "b", "c" };

        CollectionRemoval.RemoveAfterDrop(source, "b", pressedIndex: 1, target: null, targetIndex: null);

        Assert.Equal(["a", "c"], source);
    }

    [Fact]
    public void ADropOutsideRemovesThePressedIndexWhenItHoldsThePayload()
    {
        var list = new List<string> { "x", "y", "x" };

        CollectionRemoval.RemoveDroppedOutside(list, "x", pressedIndex: 2);

        Assert.Equal(["x", "y"], list);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(null)]
    public void ADropOutsideRemovesByEqualityWhenThePressedIndexDoesNotHoldThePayload(int? pressedIndex)
    {
        var list = new List<string> { "a", "b", "c" };

        CollectionRemoval.RemoveDroppedOutside(list, "c", pressedIndex);

        Assert.Equal(["a", "b"], list);
    }

    private static void Drop(IList target, object payload, int index) => target.Insert(index, payload);
}
