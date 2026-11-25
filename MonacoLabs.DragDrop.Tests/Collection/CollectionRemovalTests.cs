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

        RemoveOne(source, "b", pressedIndex: 1, target, targetIndex: 0);

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

        RemoveOne(list, payload, pressedIndex, list, targetIndex);

        Assert.Equal(expected, list);
    }

    [Fact]
    public void AnAppendWithinOneListRemovesThePressedIndex()
    {
        var list = new List<string> { "a", "b", "c" };
        list.Add("a");

        RemoveOne(list, "a", pressedIndex: 0, list, targetIndex: null);

        Assert.Equal(["b", "c", "a"], list);
    }

    [Fact]
    public void WithoutAPressedIndexTheMoveRemovesThePayloadByEquality()
    {
        var source = new List<string> { "a", "b", "c" };
        var target = new List<string>();
        Drop(target, "b", 0);

        RemoveOne(source, "b", pressedIndex: null, target, targetIndex: 0);

        Assert.Equal(["a", "c"], source);
    }

    [Fact]
    public void AStalePressedIndexFallsBackToTheFirstEqualItem()
    {
        var source = new List<string> { "a", "b", "c" };
        var target = new List<string>();
        Drop(target, "c", 0);

        RemoveOne(source, "c", pressedIndex: 0, target, targetIndex: 0);

        Assert.Equal(["a", "b"], source);
    }

    [Fact]
    public void TheEqualitySearchSkipsTheItemTheDropInserted()
    {
        var list = new List<string> { "y", "x" };
        Drop(list, "x", 0);

        RemoveOne(list, "x", pressedIndex: null, list, targetIndex: 0);

        Assert.Equal(["x", "y"], list);
    }

    [Fact]
    public void MovingOneOfTwoEqualItemsRemovesThatOne()
    {
        var list = new List<string> { "x", "y", "x" };
        Drop(list, "x", 1);

        RemoveOne(list, "x", pressedIndex: 2, list, targetIndex: 1);

        Assert.Equal(["x", "x", "y"], list);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(null)]
    public void WithoutAPressedIndexAnAppendRemovesTheOriginalNotTheAppendedItem(int? targetIndex)
    {
        var list = new List<string> { "a", "b" };
        list.Add("a");

        RemoveOne(list, "a", pressedIndex: null, list, targetIndex);

        Assert.Equal(["b", "a"], list);
    }

    [Fact]
    public void AMoveOntoATargetWithoutACollectionRemovesThePressedIndex()
    {
        var source = new List<string> { "a", "b", "c" };

        RemoveOne(source, "b", pressedIndex: 1, target: null, targetIndex: null);

        Assert.Equal(["a", "c"], source);
    }

    [Fact]
    public void SeveralOriginalsMovedToAnotherListAreAllRemoved()
    {
        var source = new List<string> { "a", "b", "c", "d" };
        var target = new List<string> { "x" };
        Drop(target, ["b", "d"], 1);

        CollectionRemoval.RemoveAfterDrop(Originals(source, ("b", 1), ("d", 3)), target, 1, 2);

        Assert.Equal(["a", "c"], source);
        Assert.Equal(["x", "b", "d"], target);
    }

    [Theory]
    [InlineData(0, new[] { "b", "d", "a", "c", "e" })]
    [InlineData(2, new[] { "a", "b", "d", "c", "e" })]
    [InlineData(4, new[] { "a", "c", "b", "d", "e" })]
    [InlineData(5, new[] { "a", "c", "e", "b", "d" })]
    [InlineData(null, new[] { "a", "c", "e", "b", "d" })]
    public void SeveralOriginalsMovedWithinOneListAreRemovedAfterTheInsertionShiftsThem(int? targetIndex, string[] expected)
    {
        var list = new List<string> { "a", "b", "c", "d", "e" };
        Drop(list, ["b", "d"], targetIndex);

        CollectionRemoval.RemoveAfterDrop(Originals(list, ("b", 1), ("d", 3)), list, targetIndex, 2);

        Assert.Equal(expected, list);
    }

    [Fact]
    public void StaleIndexesOfSeveralOriginalsFallBackToTheirFirstEqualItems()
    {
        var source = new List<string> { "x", "a", "b" };
        var target = new List<string>();
        Drop(target, ["a", "b"], 0);

        CollectionRemoval.RemoveAfterDrop(Originals(source, ("a", 0), ("b", 1)), target, 0, 2);

        Assert.Equal(["x"], source);
    }

    [Fact]
    public void EqualOriginalsRemoveOneItemEach()
    {
        var withIndexes = new List<string> { "x", "y", "x", "x" };
        var withoutIndexes = new List<string> { "x", "x", "y" };
        var target = new List<string>();

        CollectionRemoval.RemoveAfterDrop(Originals(withIndexes, ("x", 2), ("x", 3)), target, 0, 2);
        CollectionRemoval.RemoveAfterDrop(Originals(withoutIndexes, ("x", null), ("x", null)), target, 0, 2);

        Assert.Equal(["x", "y"], withIndexes);
        Assert.Equal(["y"], withoutIndexes);
    }

    [Fact]
    public void WithoutIndexesTheEqualitySearchSkipsEveryInsertedItem()
    {
        var list = new List<string> { "a", "x", "x" };
        Drop(list, ["x", "x"], 0);

        CollectionRemoval.RemoveAfterDrop(Originals(list, ("x", null), ("x", null)), list, 0, 2);

        Assert.Equal(["x", "x", "a"], list);
    }

    [Fact]
    public void OriginalsFromSeveralCollectionsAreEachRemovedFromTheirOwn()
    {
        var first = new List<string> { "a", "b" };
        var second = new List<string> { "c", "d" };
        var target = new List<string>();
        Drop(target, ["b", "c"], 0);

        CollectionRemoval.RemoveAfterDrop([.. Originals(first, ("b", 1)), .. Originals(second, ("c", 0))], target, 0, 2);

        Assert.Equal(["a"], first);
        Assert.Equal(["d"], second);
    }

    [Fact]
    public void ADropOutsideRemovesThePressedIndexWhenItHoldsThePayload()
    {
        var list = new List<string> { "x", "y", "x" };

        CollectionRemoval.RemoveDroppedOutside(Originals(list, ("x", 2)));

        Assert.Equal(["x", "y"], list);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(null)]
    public void ADropOutsideRemovesByEqualityWhenThePressedIndexDoesNotHoldThePayload(int? pressedIndex)
    {
        var list = new List<string> { "a", "b", "c" };

        CollectionRemoval.RemoveDroppedOutside(Originals(list, ("c", pressedIndex)));

        Assert.Equal(["a", "b"], list);
    }

    [Fact]
    public void ADropOutsideRemovesEveryOriginal()
    {
        var list = new List<string> { "a", "b", "c", "d" };

        CollectionRemoval.RemoveDroppedOutside(Originals(list, ("a", 0), ("c", 7)));

        Assert.Equal(["b", "d"], list);
    }

    private static void RemoveOne(IList source, string payload, int? pressedIndex, IList? target, int? targetIndex) =>
        CollectionRemoval.RemoveAfterDrop(Originals(source, (payload, pressedIndex)), target, targetIndex, 1);

    private static CollectionDragItem[] Originals(IList collection, params (string Item, int? Index)[] originals) =>
        [.. originals.Select(x => new CollectionDragItem(x.Item, collection, x.Index, null))];

    private static void Drop(IList target, object payload, int index) => target.Insert(index, payload);

    private static void Drop(IList target, string[] payloads, int? index)
    {
        for (var i = 0; i < payloads.Length; i++)
        {
            if (index is int at)
                target.Insert(at + i, payloads[i]);
            else
                target.Add(payloads[i]);
        }
    }
}
