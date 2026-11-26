using System.Collections;

namespace MonacoLabs.DragDrop;

internal static class DisplayIndexMap
{
    /// <summary>
    /// Maps a position in <paramref name="view"/>, a filtered or sorted view of <paramref name="collection"/>, to an index in the collection:
    /// the item shown at <paramref name="displayIndex"/>, matched by occurrence among equal items, or just after the last shown item when
    /// <paramref name="displayIndex"/> is the view's count. Null, meaning append, for an empty view, an index outside it, or an item the
    /// collection doesn't contain.
    /// </summary>
    public static int? ToCollectionIndex(IReadOnlyList<object?> view, IList collection, int displayIndex)
    {
        if (displayIndex < 0 || displayIndex > view.Count || view.Count == 0)
            return null;

        if (displayIndex == view.Count)
            return ToCollectionIndex(view, collection, displayIndex - 1) + 1;

        var item = view[displayIndex];
        var occurrence = 0;
        for (var i = 0; i < displayIndex; i++)
        {
            if (Equals(view[i], item))
                occurrence++;
        }

        for (var i = 0; i < collection.Count; i++)
        {
            if (Equals(collection[i], item) && occurrence-- == 0)
                return i;
        }

        return null;
    }
}
