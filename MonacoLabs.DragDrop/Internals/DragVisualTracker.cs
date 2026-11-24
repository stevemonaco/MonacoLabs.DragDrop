using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Owns the source-side visuals of one drag: the <c>:dragging</c> pseudo-class and the pointer-following preview.
/// </summary>
internal sealed class DragVisualTracker : IDisposable
{
    private readonly Control _source;
    private readonly TopLevel? _topLevel;
    private readonly AdornerLayer? _layer;
    private readonly DragPreviewAdorner? _preview;
    private readonly bool _injectedAllowDrop;

    public DragVisualTracker(Control source, Point grabOffset, bool showPreview)
    {
        _source = source;

        // Snapshot before :dragging is applied so the preview doesn't capture the source's dragging style
        if (showPreview)
        {
            _topLevel = TopLevel.GetTopLevel(source);
            _layer = AdornerLayer.GetAdornerLayer(source);
            if (_topLevel is not null && _layer is not null)
                _preview = DragPreviewAdorner.Create(source, grabOffset);
        }

        if (_preview is not null)
        {
            _preview.IsVisible = false;
            _layer!.Children.Add(_preview);

            // Drag events only reach elements that allow drops, so the preview can't track the pointer elsewhere without this
            if (!AvaDragDrop.GetAllowDrop(_topLevel!))
            {
                AvaDragDrop.SetAllowDrop(_topLevel!, true);
                _injectedAllowDrop = true;
            }

            _topLevel!.AddHandler(AvaDragDrop.DragEnterEvent, TopLevel_DragOver, RoutingStrategies.Bubble, true);
            _topLevel.AddHandler(AvaDragDrop.DragOverEvent, TopLevel_DragOver, RoutingStrategies.Bubble, true);
            _topLevel.AddHandler(AvaDragDrop.DragLeaveEvent, TopLevel_DragLeave, RoutingStrategies.Bubble, true);
            _topLevel.AddHandler(AvaDragDrop.DropEvent, TopLevel_Drop, RoutingStrategies.Bubble, true);
        }

        ((IPseudoClasses)source.Classes).Set(":dragging", true);
    }

    private void TopLevel_DragOver(object? sender, DragEventArgs e)
    {
        RejectIfNotDropTarget(e);

        _preview!.MoveTo(e.GetPosition(_layer));
        _preview.SetCanDrop(e.DragEffects != DragDropEffects.None);
        _preview.IsVisible = true;
    }

    // Moving between elements raises DragLeave then DragEnter in the same pass, so this only hides the preview when leaving the window
    private void TopLevel_DragLeave(object? sender, DragEventArgs e) => _preview!.IsVisible = false;

    private void TopLevel_Drop(object? sender, DragEventArgs e)
    {
        RejectIfNotDropTarget(e);
        _preview!.IsVisible = false;
    }

    private void RejectIfNotDropTarget(DragEventArgs e)
    {
        if (_injectedAllowDrop && !AllowsDropExplicitly(e.Source as Visual))
            e.DragEffects = DragDropEffects.None;
    }

    private bool AllowsDropExplicitly(Visual? source)
    {
        for (var visual = source; visual is not null && visual != _topLevel; visual = visual.GetVisualParent())
        {
            if (visual.IsSet(AvaDragDrop.AllowDropProperty))
                return visual.GetValue(AvaDragDrop.AllowDropProperty);
        }

        return false;
    }

    public void Dispose()
    {
        ((IPseudoClasses)_source.Classes).Set(":dragging", false);

        if (_preview is null)
            return;

        _layer!.Children.Remove(_preview);
        _topLevel!.RemoveHandler(AvaDragDrop.DragEnterEvent, TopLevel_DragOver);
        _topLevel.RemoveHandler(AvaDragDrop.DragOverEvent, TopLevel_DragOver);
        _topLevel.RemoveHandler(AvaDragDrop.DragLeaveEvent, TopLevel_DragLeave);
        _topLevel.RemoveHandler(AvaDragDrop.DropEvent, TopLevel_Drop);

        if (_injectedAllowDrop)
            _topLevel.ClearValue(AvaDragDrop.AllowDropProperty);
    }
}
