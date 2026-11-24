using Avalonia.Input;

namespace MonacoLabs.DragDrop;

internal static class DropEffectResolver
{
    public static DragDropEffects Resolve(DragDropEffects allowed, KeyModifiers modifiers, bool hasInProcessPayload, bool isMacOS)
    {
        // A Move tells an external source such as a file manager to delete its original
        if (!hasInProcessPayload)
            return allowed.HasFlag(DragDropEffects.Copy) ? DragDropEffects.Copy : allowed & DragDropEffects.Link;

        var copyModifier = isMacOS ? KeyModifiers.Alt : KeyModifiers.Control;
        var requested = (modifiers & (copyModifier | KeyModifiers.Shift)) switch
        {
            var m when m == (copyModifier | KeyModifiers.Shift) => DragDropEffects.Link,
            var m when m == copyModifier => DragDropEffects.Copy,
            KeyModifiers.Shift => DragDropEffects.Move,
            _ => DragDropEffects.None
        };

        if (requested != DragDropEffects.None && allowed.HasFlag(requested))
            return requested;

        if (allowed.HasFlag(DragDropEffects.Move))
            return DragDropEffects.Move;

        if (allowed.HasFlag(DragDropEffects.Copy))
            return DragDropEffects.Copy;

        return allowed & DragDropEffects.Link;
    }
}
