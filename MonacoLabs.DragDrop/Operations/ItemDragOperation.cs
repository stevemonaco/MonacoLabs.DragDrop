using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Drags a single value. When <see cref="DragOperationBase.Payload"/> is bound, a Move clears it (or swaps it, see <see cref="SwapOnDrop"/>),
/// so the binding should be TwoWay, which is the default on this type.
/// </summary>
public partial class ItemDragOperation : DragOperationBase
{
    static ItemDragOperation()
    {
        PayloadProperty.OverrideMetadata<ItemDragOperation>(new StyledPropertyMetadata<object?>(defaultBindingMode: BindingMode.TwoWay));
    }

    public override void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo)
    {
        if (effect != DragDropEffects.Move || !IsSet(PayloadProperty))
            return;

        SetCurrentValue(PayloadProperty, SwapOnDrop ? dropInfo.ReplacedPayload : null);
    }

    protected override void OnDroppedOutside(DragInfo dragInfo, object payload)
    {
        if (IsSet(PayloadProperty))
            SetCurrentValue(PayloadProperty, null);
    }
}
