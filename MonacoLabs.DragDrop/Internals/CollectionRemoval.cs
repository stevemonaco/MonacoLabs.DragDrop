using System.Collections;

namespace MonacoLabs.DragDrop;

internal static class CollectionRemoval
{
    /// <summary>
    /// Removes moved originals from their collections. Each is removed at its recorded index, shifted past the insertion when the drop inserted
    /// into the same collection at or before it, if that index still holds an equal item; otherwise the first equal item outside the inserted range.
    /// <paramref name="target"/>, <paramref name="targetIndex"/> and <paramref name="insertedCount"/> describe the insertion, when known.
    /// </summary>
    public static void RemoveAfterDrop(IReadOnlyList<CollectionDragItem> originals, IList? target, int? targetIndex, int insertedCount)
    {
        foreach (var (source, group) in GroupByCollection(originals))
        {
            if (!ReferenceEquals(source, target) || insertedCount <= 0)
            {
                Remove(source, group, index => index, _ => false);
                continue;
            }

            var start = targetIndex ?? source.Count - insertedCount;
            Remove(source, group,
                index => index >= start ? index + insertedCount : index,
                index => index >= start && index < start + insertedCount);
        }
    }

    /// <summary>
    /// Removes originals dropped outside any target: at the recorded index if it still holds an equal item, otherwise the first equal item.
    /// </summary>
    public static void RemoveDroppedOutside(IReadOnlyList<CollectionDragItem> originals)
    {
        foreach (var (source, group) in GroupByCollection(originals))
            Remove(source, group, index => index, _ => false);
    }

    private static IEnumerable<(IList Source, IEnumerable<CollectionDragItem> Originals)> GroupByCollection(IReadOnlyList<CollectionDragItem> originals) =>
        originals
            .Where(x => x.Collection is not null)
            .GroupBy(x => (object)x.Collection!, ReferenceEqualityComparer.Instance)
            .Select(g => ((IList)g.Key, (IEnumerable<CollectionDragItem>)g));

    // Picks every index before removing any, then removes from the highest, so earlier removals don't shift later ones
    private static void Remove(IList source, IEnumerable<CollectionDragItem> originals, Func<int, int> shift, Func<int, bool> isInserted)
    {
        var claimed = new SortedSet<int>();
        foreach (var original in originals)
        {
            if (original.Index is int index && index >= 0 && shift(index) is var shifted && shifted < source.Count
                && !claimed.Contains(shifted) && Equals(source[shifted], original.Item))
            {
                claimed.Add(shifted);
                continue;
            }

            for (var i = 0; i < source.Count; i++)
            {
                if (!isInserted(i) && !claimed.Contains(i) && Equals(source[i], original.Item))
                {
                    claimed.Add(i);
                    break;
                }
            }
        }

        foreach (var index in claimed.Reverse())
            source.RemoveAt(index);
    }
}
