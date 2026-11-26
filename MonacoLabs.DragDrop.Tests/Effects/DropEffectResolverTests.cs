using Avalonia.Input;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DropEffectResolverTests
{
    private const DragDropEffects All = DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;

    [Theory]
    [InlineData(KeyModifiers.Control | KeyModifiers.Shift, false)]
    [InlineData(KeyModifiers.Alt | KeyModifiers.Shift, true)]
    public void CopyModifierWithShiftLinks(KeyModifiers modifiers, bool isMacOS) =>
        Assert.Equal(DragDropEffects.Link, DropEffectResolver.Resolve(All, modifiers, hasInProcessPayload: true, isMacOS));

    [Theory]
    [InlineData(KeyModifiers.Control, false)]
    [InlineData(KeyModifiers.Alt, true)]
    public void CopyModifierCopies(KeyModifiers modifiers, bool isMacOS) =>
        Assert.Equal(DragDropEffects.Copy, DropEffectResolver.Resolve(All, modifiers, hasInProcessPayload: true, isMacOS));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShiftMoves(bool isMacOS) =>
        Assert.Equal(DragDropEffects.Move, DropEffectResolver.Resolve(All, KeyModifiers.Shift, hasInProcessPayload: true, isMacOS));

    [Theory]
    [InlineData(All, DragDropEffects.Move)]
    [InlineData(DragDropEffects.Copy | DragDropEffects.Link, DragDropEffects.Copy)]
    [InlineData(DragDropEffects.Copy, DragDropEffects.Copy)]
    [InlineData(DragDropEffects.Link, DragDropEffects.Link)]
    [InlineData(DragDropEffects.None, DragDropEffects.None)]
    public void WithoutModifiersMoveIsPreferredThenCopyThenLink(DragDropEffects allowed, DragDropEffects expected) =>
        Assert.Equal(expected, DropEffectResolver.Resolve(allowed, KeyModifiers.None, hasInProcessPayload: true, isMacOS: false));

    [Theory]
    [InlineData(DragDropEffects.Copy, KeyModifiers.Shift, DragDropEffects.Copy)]
    [InlineData(DragDropEffects.Move | DragDropEffects.Link, KeyModifiers.Control, DragDropEffects.Move)]
    [InlineData(DragDropEffects.Copy | DragDropEffects.Move, KeyModifiers.Control | KeyModifiers.Shift, DragDropEffects.Move)]
    [InlineData(DragDropEffects.Link, KeyModifiers.Shift, DragDropEffects.Link)]
    public void AnUnavailableRequestFallsBackToTheDefaultOrder(DragDropEffects allowed, KeyModifiers modifiers, DragDropEffects expected) =>
        Assert.Equal(expected, DropEffectResolver.Resolve(allowed, modifiers, hasInProcessPayload: true, isMacOS: false));

    [Theory]
    [InlineData(KeyModifiers.Alt, false)]
    [InlineData(KeyModifiers.Control, true)]
    public void TheOtherPlatformsCopyModifierDoesNotCopy(KeyModifiers modifiers, bool isMacOS) =>
        Assert.Equal(DragDropEffects.Move, DropEffectResolver.Resolve(All, modifiers, hasInProcessPayload: true, isMacOS));

    [Theory]
    [InlineData(KeyModifiers.Meta, DragDropEffects.Move)]
    [InlineData(KeyModifiers.Control | KeyModifiers.Meta, DragDropEffects.Copy)]
    [InlineData(KeyModifiers.Shift | KeyModifiers.Alt, DragDropEffects.Move)]
    [InlineData(KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Meta, DragDropEffects.Link)]
    public void OtherModifiersDoNotChangeTheEffect(KeyModifiers modifiers, DragDropEffects expected) =>
        Assert.Equal(expected, DropEffectResolver.Resolve(All, modifiers, hasInProcessPayload: true, isMacOS: false));

    [Theory]
    [InlineData(All, KeyModifiers.None, DragDropEffects.Copy)]
    [InlineData(All, KeyModifiers.Shift, DragDropEffects.Copy)]
    [InlineData(All, KeyModifiers.Control | KeyModifiers.Shift, DragDropEffects.Copy)]
    [InlineData(DragDropEffects.Move | DragDropEffects.Link, KeyModifiers.Shift, DragDropEffects.Link)]
    [InlineData(DragDropEffects.Move, KeyModifiers.None, DragDropEffects.None)]
    public void PlatformDataIsCopiedElseLinkedWhateverTheModifiers(DragDropEffects allowed, KeyModifiers modifiers, DragDropEffects expected) =>
        Assert.Equal(expected, DropEffectResolver.Resolve(allowed, modifiers, hasInProcessPayload: false, isMacOS: false));

    [Fact]
    public void TheEffectIsAlwaysOneAllowedEffectOrNone()
    {
        foreach (var allowed in Enumerable.Range(0, 8).Select(x => (DragDropEffects)x))
            foreach (var modifiers in Enumerable.Range(0, 16).Select(x => (KeyModifiers)x))
                foreach (var hasInProcessPayload in new[] { true, false })
                    foreach (var isMacOS in new[] { true, false })
                    {
                        var effect = DropEffectResolver.Resolve(allowed, modifiers, hasInProcessPayload, isMacOS);

                        Assert.Contains(effect, new[] { DragDropEffects.None, DragDropEffects.Copy, DragDropEffects.Move, DragDropEffects.Link });
                        Assert.True(effect == DragDropEffects.None || allowed.HasFlag(effect), $"{effect} not in {allowed}");
                        Assert.False(!hasInProcessPayload && effect == DragDropEffects.Move, "a platform drop reported Move");
                    }
    }
}
