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
