using System.Collections;

namespace MonacoLabs.DragDrop;

internal static class CollectionRemoval
{
    /// <summary>
    /// Removes a moved item from <paramref name="source"/>: at the pressed index if it still holds an equal item, otherwise the first equal item
    /// that is not the one the drop inserted. <paramref name="target"/> and <paramref name="targetIndex"/> describe the insertion, when known.
    /// </summary>
    public static void RemoveAfterDrop(IList source, object? payload, int? pressedIndex, IList? target, int? targetIndex)
    {
        var sameCollection = ReferenceEquals(source, target);

        if (pressedIndex is int index)
        {
            // Inserting into the source at or before the original shifted it down by one
            var shifted = sameCollection && index >= targetIndex ? index + 1 : index;
            if (shifted < source.Count && Equals(source[shifted], payload))
            {
                source.RemoveAt(shifted);
                return;
            }
        }

        if (payload is null)
            return;

        int? inserted = sameCollection ? targetIndex ?? source.Count - 1 : null;
        for (var i = 0; i < source.Count; i++)
        {
            if (i != inserted && Equals(source[i], payload))
            {
                source.RemoveAt(i);
                return;
            }
        }
    }

    public static void RemoveDroppedOutside(IList source, object payload, int? pressedIndex)
    {
        if (pressedIndex is int index && index < source.Count && Equals(source[index], payload))
            source.RemoveAt(index);
        else
            source.Remove(payload);
    }
}
