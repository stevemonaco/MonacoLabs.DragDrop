using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Metadata;
using MonacoLabs.DragDrop.Abstractions;
using System.Reflection;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class WiringTests
{
    [AvaloniaFact]
    public void SettingADragOperationAttachesItAfterDetachingThePreviousOne()
    {
        var control = new Border();
        var first = new ItemDragOperation();
        var second = new ItemDragOperation();

        DragDrop.SetDragOperation(control, first);
        Assert.Same(control, first.AttachedControl);

        DragDrop.SetDragOperation(control, second);
        Assert.Null(first.AttachedControl);
        Assert.Same(control, second.AttachedControl);
    }

    [AvaloniaFact]
    public void SettingADropOperationAttachesItAfterDetachingThePreviousOne()
    {
        var control = new Border();
        var first = new ItemDropOperation();
        var second = new ItemDropOperation();

        DragDrop.SetDropOperation(control, first);
        Assert.Same(control, first.AttachedControl);

        DragDrop.SetDropOperation(control, second);
        Assert.Null(first.AttachedControl);
        Assert.Same(control, second.AttachedControl);
    }

    [AvaloniaFact]
    public void ClearingTheAttachedPropertiesDetachesTheOperations()
    {
        var control = new Border();
        var drag = new ItemDragOperation();
        var drop = new ItemDropOperation();
        DragDrop.SetDragOperation(control, drag);
        DragDrop.SetDropOperation(control, drop);

        control.ClearValue(DragDrop.DragOperationProperty);
        control.ClearValue(DragDrop.DropOperationProperty);

        Assert.Null(drag.AttachedControl);
        Assert.Null(drop.AttachedControl);
    }

    [Fact]
    public void TheOperationPropertiesDefaultToOneTimeBinding()
    {
        Assert.Equal(BindingMode.OneTime, DragDrop.DragOperationProperty.GetMetadata(typeof(Control)).DefaultBindingMode);
        Assert.Equal(BindingMode.OneTime, DragDrop.DropOperationProperty.GetMetadata(typeof(Control)).DefaultBindingMode);
    }

    [AvaloniaFact]
    public void AControlCanHoldADragAndADropOperation()
    {
        var control = new Border();
        var drag = new ItemDragOperation();
        var drop = new ItemDropOperation();

        DragDrop.SetDragOperation(control, drag);
        DragDrop.SetDropOperation(control, drop);

        Assert.Same(control, drag.AttachedControl);
        Assert.Same(control, drop.AttachedControl);
    }

    [AvaloniaFact]
    public void DetachingADetachedOperationDoesNothing()
    {
        var drag = new ItemDragOperation();
        var drop = new ItemDropOperation();

        drag.Detach(new Border());
        drop.Detach(new Border());

        Assert.Null(drag.AttachedControl);
        Assert.Null(drop.AttachedControl);
    }

    [AvaloniaFact]
    public void AttachingAnAttachedOperationThrowsInvalidOperation()
    {
        var control = new Border { Name = "Source" };
        var drag = new ItemDragOperation();
        var drop = new ItemDropOperation();
        var description = $"{control}";
        drag.Attach(control);
        drop.Attach(control);

        var dragError = Assert.Throws<InvalidOperationException>(() => drag.Attach(new Border()));
        var dropError = Assert.Throws<InvalidOperationException>(() => drop.Attach(new Border()));

        Assert.Contains(nameof(ItemDragOperation), dragError.Message);
        Assert.Contains(description, dragError.Message);
        Assert.Contains(nameof(ItemDropOperation), dropError.Message);
        Assert.Contains(description, dropError.Message);
        Assert.Same(control, drag.AttachedControl);
        Assert.Same(control, drop.AttachedControl);
    }
    [AvaloniaFact]
    public void ADetachedOperationCanAttachToAnotherControl()
    {
        var first = new Border();
        var second = new Border();
        var drag = new ItemDragOperation();
        var drop = new ItemDropOperation();
        drag.Attach(first);
        drop.Attach(first);

        drag.Detach(first);
        drop.Detach(first);
        drag.Attach(second);
        drop.Attach(second);

        Assert.Same(second, drag.AttachedControl);
        Assert.Same(second, drop.AttachedControl);
    }

    [AvaloniaFact]
    public void IsDragHandleDefaultsToFalse() => Assert.False(DragDrop.GetIsDragHandle(new Border()));

    [Fact]
    public void OnlyTheRootNamespaceIsMappedToTheXmlNamespace()
    {
        var mapping = Assert.Single(typeof(DragDrop).Assembly.GetCustomAttributes<XmlnsDefinitionAttribute>());

        Assert.Equal("https://github.com/stevemonaco/MonacoLabs.DragDrop", mapping.XmlNamespace);
        Assert.Equal("MonacoLabs.DragDrop", mapping.ClrNamespace);
    }

    [Fact]
    public void DragDropIsAStaticClass() => Assert.True(typeof(DragDrop).IsAbstract && typeof(DragDrop).IsSealed);

    [AvaloniaFact]
    public void AttachedControlChangesOnlyThroughAttachAndDetach()
    {
        AssertReadOnly(new ItemDragOperation(), DragOperationBase.AttachedControlProperty, typeof(DragOperationBase));
        AssertReadOnly(new ItemDropOperation(), DropOperationBase.AttachedControlProperty, typeof(DropOperationBase));

        static void AssertReadOnly(AvaloniaObject operation, AvaloniaProperty property, Type owner)
        {
            Assert.True(property.IsReadOnly);
            Assert.False(owner.GetProperty("AttachedControl")!.SetMethod?.IsPublic ?? false);
            Assert.ThrowsAny<Exception>(() => operation.SetValue(property, new Border()));
        }
    }

    [AvaloniaFact]
    public void AttachedControlRaisesAPropertyChange()
    {
        var control = new Border();
        var drag = new ItemDragOperation();
        var drop = new ItemDropOperation();
        var changes = new List<(object? Old, object? New)>();
        drag.PropertyChanged += (_, e) => { if (e.Property == DragOperationBase.AttachedControlProperty) changes.Add((e.OldValue, e.NewValue)); };
        drop.PropertyChanged += (_, e) => { if (e.Property == DropOperationBase.AttachedControlProperty) changes.Add((e.OldValue, e.NewValue)); };

        drag.Attach(control);
        drag.Detach(control);
        drop.Attach(control);
        drop.Detach(control);

        Assert.Equal([(null, control), (control, null), (null, control), (control, null)], changes);
    }
}
