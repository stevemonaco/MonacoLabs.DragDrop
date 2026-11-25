using Avalonia.Input;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Drops a single value: an accepted in-process drop replaces <see cref="DropOperationBase.PayloadTarget"/> with the payload.
/// Drags carrying more than one payload are not meant for it unless <see cref="DropOperationBase.AcceptsMultiplePayloads"/> is set.
/// </summary>
public class ItemDropOperation : DropOperationBase
{
    static ItemDropOperation()
    {
        AcceptsMultiplePayloadsProperty.OverrideDefaultValue<ItemDropOperation>(false);
    }

    /// <summary>
    /// Sets <see cref="DropOperationBase.PayloadTarget"/> to the first of <paramref name="values"/> and reports the previous value as
    /// <see cref="DropInfo.ReplacedPayload"/>.
    /// </summary>
    protected override DropInfo? Transfer(DragEventArgs e, DragInfo dragInfo, IReadOnlyList<object> values)
    {
        var replaced = PayloadTarget;
        SetCurrentValue(PayloadTargetProperty, values[0]);

        return new DropInfo
        {
            DragEventArgs = e,
            HoveredControl = AttachedControl!,
            HoverLocation = e.GetPosition(AttachedControl!),
            ReplacedPayload = replaced
        };
    }
}
