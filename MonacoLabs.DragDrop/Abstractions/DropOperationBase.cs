using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Styling;
using Avalonia.Threading;
using MonacoLabs.DragDrop.Adorners;
using AvaDragDrop = Avalonia.Input.DragDrop;
using System.Diagnostics;

namespace MonacoLabs.DragDrop.Abstractions;

public abstract partial class DropOperationBase : AvaloniaObject, IDropOperation
{
    public IList<string> InteractionIds { get; set; } = [DragDropIds.DefaultOperation];
    public object? Context { get; set; }
    public RoutingStrategies Routing { get; set; } = RoutingStrategies.Bubble;
    protected bool _handledEventsToo = false;

    protected DragDropEffects _lastEffects;
    private bool _isDragLeavePending;

    protected DropAdornerKind DropAdornerType { get; set; } = DropAdornerKind.Solid;
    
    public virtual void Attach(Control control)
    {
        ThrowIf.NotNull(AttachedControl);
        
        AttachedControl = control;
        AvaDragDrop.SetAllowDrop(control, true);
        SubscribeDropEvents(control);

        DropAdorner ??= new DropHighlightAdorner(DropAdornerType);
        DropAdorner.TargetControl = AttachedControl;
    }

    public virtual void Detach(Control control)
    {
        if (AttachedControl is null)
            return;

        AvaDragDrop.SetAllowDrop(control, false);
        UnsubscribeDropEvents(control);
        AttachedControl = null;
    }

    protected virtual void SubscribeDropEvents(Control control)
    {
        control.AddHandler(AvaDragDrop.DragEnterEvent, DragEnter, Routing, _handledEventsToo);
        control.AddHandler(AvaDragDrop.DragLeaveEvent, DragLeave, Routing, _handledEventsToo);
        control.AddHandler(AvaDragDrop.DragOverEvent, DragOver, Routing, _handledEventsToo);
        control.AddHandler(AvaDragDrop.DropEvent, Drop, Routing, _handledEventsToo);
    }

    protected virtual void UnsubscribeDropEvents(Control control)
    {
        control.RemoveHandler(AvaDragDrop.DragEnterEvent, DragEnter);
        control.RemoveHandler(AvaDragDrop.DragLeaveEvent, DragLeave);
        control.RemoveHandler(AvaDragDrop.DragOverEvent, DragOver);
        control.RemoveHandler(AvaDragDrop.DropEvent, Drop);
    }

    protected virtual void DragEnter(object? sender, DragEventArgs e)
    {
        CancelPendingDragLeave();
        bool canDrop = CanDrop(e);
        e.DragEffects = OnDragEnter(e, canDrop);
        _lastEffects = e.DragEffects;
    }

    // Moving between children raises DragLeave then DragEnter in one pass, so leave only if no DragEnter follows
    protected virtual void DragLeave(object? sender, DragEventArgs e)
    {
        _isDragLeavePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (!_isDragLeavePending)
                return;

            _isDragLeavePending = false;
            OnDragLeave(e);
        });
    }

    protected void CancelPendingDragLeave() => _isDragLeavePending = false;

    /// <summary>
    /// Picks the effect for a drop from the effects the drag source allows, preferring Move, then Copy, then Link.
    /// </summary>
    protected virtual DragDropEffects GetDropEffect(DragEventArgs e)
    {
        if (e.DragEffects.HasFlag(DragDropEffects.Move))
            return DragDropEffects.Move;

        if (e.DragEffects.HasFlag(DragDropEffects.Copy))
            return DragDropEffects.Copy;

        return e.DragEffects & DragDropEffects.Link;
    }

    protected void InvokePayloadCommand(DragEventArgs e, object payload, DropTargetOffset offset)
    {
        if (PayloadCommand is null)
        {
            return;
        }

        object? targetContext = null;
        if (e.Source is StyledElement layoutable)
        {
            targetContext = layoutable.DataContext;
        }
            
        DropEventArgs args = new(e.Source, targetContext, [payload], offset);
        
        if (PayloadCommand.CanExecute(args))
            PayloadCommand.Execute(args);
    }

    /// <summary>
    /// Invoked when a drag enters the AttachedControl
    /// </summary>
    /// <param name="dragEventArgs"></param>
    /// <param name="canDrop"></param>
    protected virtual DragDropEffects OnDragEnter(DragEventArgs dragEventArgs, bool canDrop)
    {
        // Drags for other interaction ids are not meant for this target, so only failed validation shows the error adorner
        if (!CanGetPayload(dragEventArgs))
            return DragDropEffects.None;

        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = canDrop;
            DropAdorner.Attach();
        }

        if (!canDrop)
            return DragDropEffects.None;

        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", true);
        return GetDropEffect(dragEventArgs);
    }

    protected virtual DragDropEffects OnDragOver()
    {
        return _lastEffects;
    }

    /// <summary>
    /// Invoked when a drag leaves the AttachedControl
    /// </summary>
    /// <param name="dragEventArgs"></param>
    protected virtual void OnDragLeave(DragEventArgs dragEventArgs)
    {
        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", false);
        DropAdorner?.Detach();
    }

    /// <summary>
    /// Invoked when a drag is over the AttachedControl
    /// This event fires repeatedly while the drag is over the control, so it should
    /// be reasonably efficient with validation
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void DragOver(object? sender, DragEventArgs e)
    {
        bool canDrop = CanDrop(e);

        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = canDrop;
            DropAdorner.InvalidateVisual();
        }

        if (!canDrop)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = OnDragOver();
    }

    /// <summary>
    /// Completes the DragDrop operation by assigning payload and cleaning up visuals
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void Drop(object? sender, DragEventArgs e)
    {
        CancelPendingDragLeave();

        if (!TryGetDragInfo<DragInfo>(e, out var dragInfo))
            return;

        if (!TryGetPayload<object>(e, out var payload))
            return;

        if (!CanDrop(e))
        {
            e.DragEffects = DragDropEffects.None;
            OnDragLeave(e);
            return;
        }

        e.DragEffects = GetDropEffect(e);
        SetCurrentValue(PayloadTargetProperty, payload);

        var dropMetadata = new DropInfo()
        {
            DragEventArgs = e,
            HoveredControl = (Control)sender
        };

        dragInfo.DragOperation.DropCompleted(e.DragEffects, dragInfo, dropMetadata);
        
        InvokePayloadCommand(e, payload, DropTargetOffset.OnTarget);

        OnDrop(e);

        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", false);

        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = false;
            DropAdorner.Detach();
        }
    }
    
    protected virtual void OnDrop(DragEventArgs dragEventArgs)
    {
    }

    /// <summary>
    /// Checks if the drop is expected to be valid
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected virtual bool CanDrop(DragEventArgs e)
    {
        //var metadata = GetMetadata(e);
        var hasPayload = CanGetPayload(e);

        return hasPayload;
    }

    /// <summary>
    /// Checks if the drag payload is compatible with the DropTarget
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected virtual bool CanGetPayload(DragEventArgs e)
    {
        return InteractionIds.Any(x => e.DataTransfer.Contains(DragDropIds.PayloadFormat(x)));
    }

    /// <summary>
    /// Tries to get the Payload to be assigned to the DropTarget
    /// Overriding this allows a data transform from the Payload set by the Drag operation into
    /// what the DropTarget expects, if it's not 1:1 compatible
    /// </summary>
    /// <typeparam name="TPayload">Compatible type of payload expected</typeparam>
    /// <param name="e">Drag event that started the operation</param>
    /// <param name="payload">Payload to be transferred by the operation</param>
    /// <returns></returns>
    protected virtual bool TryGetPayload<TPayload>(DragEventArgs e, [MaybeNullWhen(false)] out TPayload payload)
        where TPayload : class
    {
        if (InteractionIds.Select(x => e.DataTransfer.TryGetValue(DragDropIds.PayloadFormat(x))).OfType<object>().FirstOrDefault() is TPayload p)
        {
            payload = p;
            return true;
        }

        payload = null;
        return false;
    }

    /// <summary>
    /// Tries to get metadata related to the initiation of the drag operation
    /// </summary>
    /// <typeparam name="TDragMetadata">Compatible type of drag metadata</typeparam>
    /// <param name="e">Drag event that started the operation</param>
    /// <param name="dragMetadata">Metadata from the drag initiation</param>
    /// <returns></returns>
    protected bool TryGetDragInfo<TDragMetadata>(DragEventArgs e, [MaybeNullWhen(false)] out TDragMetadata dragMetadata)
        where TDragMetadata : DragInfo
    {
        if (e.DataTransfer.TryGetValue(DragDropIds.DragMetadataFormat) is TDragMetadata metadata)
        {
            dragMetadata = metadata;
            return true;
        }

        dragMetadata = null;
        return false;
    }
}
