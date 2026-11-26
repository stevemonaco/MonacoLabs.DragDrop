using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DragDropSample.ViewModels;

public partial class SingleItemPageViewModel : PageViewModel
{
    public IReadOnlyList<MenuItemViewModel> Meals { get; } =
    [
        new("Margherita pizza", "🍕", 12.50m),
        new("Street tacos", "🌮", 9.75m),
        new("Ribeye steak", "🥩", 24.00m, IsSoldOut: true),
        new("Garden salad", "🥗", 8.25m),
    ];

    public IReadOnlyList<MenuItemViewModel> Drinks { get; } =
    [
        new("Sparkling water", "💧", 2.00m),
        new("Green tea", "🍵", 3.25m),
        new("Cola", "🥤", 2.75m),
        new("Orange juice", "🧃", 3.50m),
    ];

    public IReadOnlyList<GuestOrderViewModel> Guests { get; } = [new("Guest 1"), new("Guest 2")];

    public decimal Total => Guests.Sum(x => x.Subtotal);

    public SingleItemPageViewModel()
    {
        Title = "Single Item";
        Description = "Drag a meal or drink onto a guest's order. Drag a filled slot by its grip to move it (or swap with an occupied slot), "
            + "hold Ctrl to copy, or drop it on empty space to remove it. Sold-out items and locked orders are rejected.";

        foreach (var guest in Guests)
            guest.PropertyChanged += Guest_PropertyChanged;
    }

    private void Guest_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GuestOrderViewModel.Subtotal))
            OnPropertyChanged(nameof(Total));
    }
}
