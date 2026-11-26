using DragDropSample.ViewModels.Nodes;
using System.Collections.ObjectModel;
using System.Linq;

namespace DragDropSample.ViewModels;

public class TreeToTreePageViewModel : PageViewModel
{
    public ObservableCollection<NodeViewModel> Workspace { get; }
    public ObservableCollection<NodeViewModel> Archive { get; }

    public TreeToTreePageViewModel()
    {
        Title = "Tree ↔ Tree";
        Description = "Move nodes within or between trees. Hover over the middle of a collapsed folder to expand it, and drag near the top or bottom "
            + "edge to scroll. A node can't be dropped into its own subtree.";

        Workspace =
        [
            Project("Inventory.Api", true,
                Folder("Controllers", true, "ItemsController.cs", "OrdersController.cs", "SuppliersController.cs", "HealthController.cs"),
                Folder("Models", false, "Item.cs", "Order.cs", "OrderLine.cs", "Supplier.cs", "Warehouse.cs"),
                Folder("Services", false,
                    Folder("Pricing", false, "PriceCalculator.cs", "DiscountRules.cs"),
                    Folder("Shipping", false, "RateTable.cs", "CarrierClient.cs")),
                File("Program.cs"),
                File("appsettings.json")),
            Project("Inventory.Web", true,
                Folder("Pages", true, "Index.razor", "Items.razor", "Orders.razor", "Reports.razor"),
                Folder("Components", false, "ItemCard.razor", "OrderTable.razor", "Pager.razor"),
                Folder("wwwroot", false, "site.css", "app.js", "favicon.ico"),
                File("App.razor"),
                File("Program.cs")),
            Project("Inventory.Tests", false,
                File("ItemsControllerTests.cs"),
                File("PriceCalculatorTests.cs"),
                File("OrderTests.cs")),
        ];

        Archive =
        [
            Project("Legacy.Reports", true,
                Folder("Exports", false, "CsvExporter.cs", "PdfExporter.cs"),
                File("ReportScheduler.cs")),
            Project("Prototypes", true,
                File("Scratch.cs"),
                File("Notes.md")),
        ];
    }

    private static ProjectNodeViewModel Project(string name, bool isExpanded, params NodeViewModel[] children) =>
        new(name) { IsExpanded = isExpanded, Children = new(children) };

    private static FolderNodeViewModel Folder(string name, bool isExpanded, params NodeViewModel[] children) =>
        new(name) { IsExpanded = isExpanded, Children = new(children) };

    private static FolderNodeViewModel Folder(string name, bool isExpanded, params string[] files) =>
        Folder(name, isExpanded, files.Select(File).ToArray());

    private static FileNodeViewModel File(string name) => new(name);
}
