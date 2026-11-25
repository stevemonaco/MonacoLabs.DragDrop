using Avalonia.Input;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Drops a single value: an accepted in-process drop replaces <see cref="DropOperationBase.PayloadTarget"/> with the payload.
/// </summary>
public class ItemDropOperation : DropOperationBase
{
    /// <summary>
    /// Sets <see cref="DropOperationBase.PayloadTarget"/> to <paramref name="value"/> and reports the previous value as <see cref="DropInfo.ReplacedPayload"/>.
    /// </summary>
    protected override DropInfo? Transfer(DragEventArgs e, DragInfo dragInfo, object value)
    {
        var replaced = PayloadTarget;
        SetCurrentValue(PayloadTargetProperty, value);

        return new DropInfo
        {
            DragEventArgs = e,
            HoveredControl = AttachedControl!,
            HoverLocation = e.GetPosition(AttachedControl!),
            ReplacedPayload = replaced
        };
    }
}
