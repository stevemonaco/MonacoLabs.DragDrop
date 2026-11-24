using System.Collections;

namespace MonacoLabs.DragDrop;

internal static class CollectionRemoval
{
    public static void RemoveAfterDrop(IList source, object? payload, int? pressedIndex, IList target, int? targetIndex)
    {
        if (pressedIndex is int index)
        {
            // Inserting into the source at or before the original shifted it down by one
            var shifted = ReferenceEquals(source, target) && index >= targetIndex;
            source.RemoveAt(shifted ? index + 1 : index);
        }
        else if (payload is not null)
        {
            source.Remove(payload);
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
