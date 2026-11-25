using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Owns the source-side state of one drag: the <c>:dragging</c> pseudo-class, the pointer-following preview,
/// and whether the drag was dropped on a part of the source's window that isn't a drop target.
/// </summary>
internal sealed class DragSession : IDisposable
{
    private readonly IReadOnlyList<Control> _draggingControls;
    private readonly TopLevel? _topLevel;
    private readonly AdornerLayer? _layer;
    private readonly DragPreviewAdorner? _preview;
    private readonly bool _injectedAllowDrop;
    private readonly bool _trackDrops;

    public bool DroppedOutsideTarget { get; private set; }

    public DragSession(TopLevel? topLevel, Control? source, IReadOnlyList<Control> draggingControls, Point grabOffset, int payloadCount,
        bool showPreview, bool trackDrops)
    {
        _draggingControls = draggingControls;
        _topLevel = topLevel;
        _trackDrops = trackDrops;

        // Snapshot before :dragging is applied so the preview doesn't capture the source's dragging style
        if (showPreview && source is not null && topLevel is not null)
        {
            _layer = AdornerLayer.GetAdornerLayer(source);
            if (_layer is not null)
                _preview = DragPreviewAdorner.Create(source, grabOffset, payloadCount);
        }

        if (_preview is not null)
        {
            _preview.IsVisible = false;
            _layer!.Children.Add(_preview);
        }

        if (_topLevel is not null && (_preview is not null || trackDrops))
        {
            // Drag events only reach elements that allow drops, so the window can't observe the pointer elsewhere without this
            if (!AvaDragDrop.GetAllowDrop(_topLevel))
            {
                AvaDragDrop.SetAllowDrop(_topLevel, true);
                _injectedAllowDrop = true;
            }

            _topLevel.AddHandler(AvaDragDrop.DragEnterEvent, TopLevel_DragOver, RoutingStrategies.Bubble, true);
            _topLevel.AddHandler(AvaDragDrop.DragOverEvent, TopLevel_DragOver, RoutingStrategies.Bubble, true);
            _topLevel.AddHandler(AvaDragDrop.DragLeaveEvent, TopLevel_DragLeave, RoutingStrategies.Bubble, true);
            _topLevel.AddHandler(AvaDragDrop.DropEvent, TopLevel_Drop, RoutingStrategies.Bubble, true);
        }

        foreach (var control in _draggingControls)
            ((IPseudoClasses)control.Classes).Set(":dragging", true);
    }

    private void TopLevel_DragOver(object? sender, DragEventArgs e)
    {
        var isOutsideTarget = ApplyEffectOutsideTargets(e);

        if (_preview is null)
            return;

        _preview.MoveTo(e.GetPosition(_layer!));
        _preview.SetEffect(e.DragEffects, isOutsideTarget && _trackDrops);
        _preview.IsVisible = true;
    }

    // Moving between elements raises DragLeave then DragEnter in the same pass, so this only hides the preview when leaving the window
    private void TopLevel_DragLeave(object? sender, DragEventArgs e)
    {
        if (_preview is not null)
            _preview.IsVisible = false;
    }

    private void TopLevel_Drop(object? sender, DragEventArgs e)
    {
        DroppedOutsideTarget = ApplyEffectOutsideTargets(e) && _trackDrops;

        if (_preview is not null)
            _preview.IsVisible = false;
    }

    /// <summary>
    /// Rejects drops over areas without a drop target, or accepts them when tracking drops so the platform raises Drop there.
    /// Returns whether the pointer is over such an area.
    /// </summary>
    private bool ApplyEffectOutsideTargets(DragEventArgs e)
    {
        if (!_injectedAllowDrop || AllowsDropExplicitly(e.Source as Visual))
            return false;

        // Win32 raises DragLeave instead of Drop when the last DragOver returned None
        e.DragEffects = _trackDrops ? PickRemoveEffect(e.DragEffects) : DragDropEffects.None;
        return true;
    }

    private static DragDropEffects PickRemoveEffect(DragDropEffects allowed)
    {
        if (allowed.HasFlag(DragDropEffects.Move))
            return DragDropEffects.Move;

        return allowed.HasFlag(DragDropEffects.Copy) ? DragDropEffects.Copy : allowed & DragDropEffects.Link;
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
        foreach (var control in _draggingControls)
            ((IPseudoClasses)control.Classes).Set(":dragging", false);

        if (_preview is not null)
        {
            _layer!.Children.Remove(_preview);
            _preview.Dispose();
        }

        if (_topLevel is null)
            return;

        _topLevel.RemoveHandler(AvaDragDrop.DragEnterEvent, TopLevel_DragOver);
        _topLevel.RemoveHandler(AvaDragDrop.DragOverEvent, TopLevel_DragOver);
        _topLevel.RemoveHandler(AvaDragDrop.DragLeaveEvent, TopLevel_DragLeave);
        _topLevel.RemoveHandler(AvaDragDrop.DropEvent, TopLevel_Drop);

        if (_injectedAllowDrop)
            _topLevel.ClearValue(AvaDragDrop.AllowDropProperty);
    }
}
