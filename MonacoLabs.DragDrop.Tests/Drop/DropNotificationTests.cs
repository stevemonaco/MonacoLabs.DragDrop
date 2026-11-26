using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections.ObjectModel;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DropNotificationTests
{
    public static TheoryData<string> Kinds => ["item", "collection", "tree"];

    [AvaloniaTheory]
    [MemberData(nameof(Kinds))]
    public void EveryTargetRaisesEnterThenOverThenDrop(string kind)
    {
        var scene = Scene.Show(kind);
        using var drag = scene.Window.SimulateDrag(scene.Data);

        drag.Enter(scene.Point);
        drag.Over(scene.Point);
        drag.Over(scene.Point);
        drag.Drop(scene.Point);

        Assert.Equal(["Enter", "Over", "Over", "Drop"], scene.Log.Names);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Kinds))]
    public void NotificationsSeeTheFeedbackOfTheirEvent(string kind)
    {
        var scene = Scene.Show(kind);
        using var drag = scene.Window.SimulateDrag(scene.Data);

        drag.Enter(scene.Point);
        drag.Over(scene.Point);
        drag.Drop(scene.Point);

        Assert.All(scene.Log.Entries, e => Assert.True(e.IsDropValid && e.HasDropover, $"{e.Name} ran before its feedback was updated"));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Kinds))]
    public void EveryTargetRaisesLeaveWhenTheDragLeaves(string kind)
    {
        var scene = Scene.Show(kind);
        using var drag = scene.Window.SimulateDrag(scene.Data);

        drag.Enter(scene.Point);
        drag.Leave(scene.Point);

        Assert.Equal(["Enter", "Leave"], scene.Log.Names);
        var leave = scene.Log.Entries[^1];
        Assert.False(leave.IsDropValid || leave.HasDropover);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Kinds))]
    public void ARejectedDropRaisesLeaveNotDrop(string kind)
    {
        var scene = Scene.Show(kind);
        scene.Log.Rejection = "No";
        using var drag = scene.Window.SimulateDrag(scene.Data);

        drag.Enter(scene.Point);
        drag.Drop(scene.Point);

        Assert.Equal(["Enter", "Leave"], scene.Log.Names);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Kinds))]
    public void ADragThatStopsBeingMeantForTheTargetRaisesLeave(string kind)
    {
        var scene = Scene.Show(kind);
        using var drag = scene.Window.SimulateDrag(scene.Data);
        drag.Enter(scene.Point);

        using var other = scene.Window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = ["other"] }, "x"));
        other.Over(scene.Point);

        Assert.Equal(["Enter", "Leave"], scene.Log.Names);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Kinds))]
    public void DetachingMidDragRaisesLeave(string kind)
    {
        var scene = Scene.Show(kind);
        using var drag = scene.Window.SimulateDrag(scene.Data);
        drag.Enter(scene.Point);

        DragDrop.SetDropOperation(scene.Target, null);

        Assert.Equal(["Enter", "Leave"], scene.Log.Names);
    }

    [AvaloniaFact]
    public void ADragThatIsNeverMeantForTheTargetRaisesNothing()
    {
        var scene = Scene.Show("item");
        using var drag = scene.Window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = ["other"] }, "x"));

        drag.Enter(scene.Point);
        drag.Over(scene.Point);
        drag.Leave(scene.Point);

        Assert.Empty(scene.Log.Names);
    }

    private sealed record Scene(Window Window, Control Target, Point Point, IDataTransfer Data, NotificationLog Log)
    {
        public static Scene Show(string kind)
        {
            var log = new NotificationLog();
            return kind switch
            {
                "item" => ShowItem(log),
                "collection" => ShowCollection(log),
                _ => ShowTree(log),
            };
        }

        private static Scene ShowItem(NotificationLog log)
        {
            var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
            DragDrop.SetDropOperation(target, new ItemTarget(log));
            var window = TestWindow.Show(target);
            return new(window, target, window.PointIn(target), DragData.For(new ItemDragOperation(), "x"), log);
        }

        private static Scene ShowCollection(NotificationLog log)
        {
            var items = new ObservableCollection<string> { "a", "b" };
            var list = new ListBox
            {
                ItemsSource = items,
                Width = 150,
                Height = 250,
                ItemTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock { Height = 30 })
            };
            DragDrop.SetDropOperation(list, new CollectionTarget(log) { TargetCollection = items });
            var window = TestWindow.Show(list);
            var container = list.ContainerFromIndex(1)!;
            var point = window.PointIn(container, new Point(10, container.Bounds.Height / 4));
            return new(window, list, point, DragData.For(new CollectionDragOperation(), "x"), log);
        }

        private static Scene ShowTree(NotificationLog log)
        {
            var tree = new TestTree(new TreeTarget(log));
            return new(tree.Window, tree.View, tree.At(tree.B, 0.1), DragData.For(new TreeDragOperation(), new TreeNode("X")), log);
        }
    }

    private sealed class NotificationLog
    {
        public string? Rejection { get; set; }

        public List<(string Name, bool IsDropValid, bool HasDropover)> Entries { get; } = [];

        public IEnumerable<string> Names => Entries.Select(e => e.Name);

        public void Add(string name, bool isDropValid, Control? control) =>
            Entries.Add((name, isDropValid, control?.Classes.Contains(":dropover") ?? false));
    }

    private sealed class ItemTarget(NotificationLog log) : ItemDropOperation
    {
        protected override bool CanDrop(DragEventArgs e) => log.Rejection is null ? base.CanDrop(e) : Reject(log.Rejection);
        protected override void OnDragEnter(DragEventArgs e) => log.Add("Enter", IsDropValid, AttachedControl);
        protected override void OnDragOver(DragEventArgs e) => log.Add("Over", IsDropValid, AttachedControl);
        protected override void OnDragLeave(DragEventArgs e) => log.Add("Leave", IsDropValid, AttachedControl);
        protected override void OnDrop(DragEventArgs e) => log.Add("Drop", IsDropValid, AttachedControl);
    }

    private sealed class CollectionTarget(NotificationLog log) : CollectionDropOperation
    {
        protected override bool CanDrop(DragEventArgs e) => log.Rejection is null ? base.CanDrop(e) : Reject(log.Rejection);
        protected override void OnDragEnter(DragEventArgs e) => log.Add("Enter", IsDropValid, AttachedControl);
        protected override void OnDragOver(DragEventArgs e) => log.Add("Over", IsDropValid, AttachedControl);
        protected override void OnDragLeave(DragEventArgs e) => log.Add("Leave", IsDropValid, AttachedControl);
        protected override void OnDrop(DragEventArgs e) => log.Add("Drop", IsDropValid, AttachedControl);
    }

    private sealed class TreeTarget(NotificationLog log) : TreeDropOperation
    {
        protected override bool CanDrop(DragEventArgs e) => log.Rejection is null ? base.CanDrop(e) : Reject(log.Rejection);
        protected override void OnDragEnter(DragEventArgs e) => log.Add("Enter", IsDropValid, AttachedControl);
        protected override void OnDragOver(DragEventArgs e) => log.Add("Over", IsDropValid, AttachedControl);
        protected override void OnDragLeave(DragEventArgs e) => log.Add("Leave", IsDropValid, AttachedControl);
        protected override void OnDrop(DragEventArgs e) => log.Add("Drop", IsDropValid, AttachedControl);
    }
}
