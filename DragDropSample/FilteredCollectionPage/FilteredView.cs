using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DragDropSample.ViewModels;

/// <summary>
/// A filtered, optionally sorted copy of a source collection. It follows the source with inserts, removals and moves rather than resets,
/// so a list showing it keeps its selection and item containers across drops.
/// </summary>
public sealed class FilteredView<T> where T : class
{
    private readonly ObservableCollection<T> _source;
    private Func<T, bool> _filter = _ => true;
    private Func<IEnumerable<T>, IEnumerable<T>>? _sort;

    public FilteredView(ObservableCollection<T> source)
    {
        _source = source;
        _source.CollectionChanged += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<T> Items { get; } = [];

    public void Update(Func<T, bool> filter, Func<IEnumerable<T>, IEnumerable<T>>? sort)
    {
        _filter = filter;
        _sort = sort;
        Refresh();
    }

    private void Refresh()
    {
        var shown = _source.Where(_filter);
        var wanted = (_sort?.Invoke(shown) ?? shown).ToList();
        var keep = new HashSet<T>(wanted, ReferenceEqualityComparer.Instance);

        for (var i = Items.Count - 1; i >= 0; i--)
        {
            if (!keep.Contains(Items[i]))
                Items.RemoveAt(i);
        }

        for (var i = 0; i < wanted.Count; i++)
        {
            if (i < Items.Count && ReferenceEquals(Items[i], wanted[i]))
                continue;

            var current = IndexOf(wanted[i], i);
            if (current >= 0)
                Items.Move(current, i);
            else
                Items.Insert(i, wanted[i]);
        }

        // A move within the source inserts the item before removing it, so the same item can briefly appear twice
        while (Items.Count > wanted.Count)
            Items.RemoveAt(Items.Count - 1);
    }

    private int IndexOf(T item, int start)
    {
        for (var i = start; i < Items.Count; i++)
        {
            if (ReferenceEquals(Items[i], item))
                return i;
        }

        return -1;
    }
}
