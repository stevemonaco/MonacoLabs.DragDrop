using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    public partial MenuItemViewModel? SelectedMeal { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    public partial MenuItemViewModel? SelectedDrink { get; set; }

    public decimal Total => (SelectedMeal?.Price ?? 0) + (SelectedDrink?.Price ?? 0);

    public SingleItemPageViewModel()
    {
        Title = "Single Item";
        Description = "Drag a meal or a drink onto your order. Each slot only accepts its own kind, and sold-out items are rejected with a reason.";
    }

    [RelayCommand]
    public void RemoveMeal()
    {
        SelectedMeal = null;
    }

    [RelayCommand]
    public void RemoveDrink()
    {
        SelectedDrink = null;
    }
}
