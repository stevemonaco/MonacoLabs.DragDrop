using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DragDropSample.ViewModels;
public partial class GuestOrderViewModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    public partial MenuItemViewModel? Meal { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    public partial MenuItemViewModel? Drink { get; set; }

    [ObservableProperty]
    public partial bool IsLocked { get; set; }

    public decimal Subtotal => (Meal?.Price ?? 0) + (Drink?.Price ?? 0);

    public GuestOrderViewModel(string name)
    {
        Name = name;
    }

    [RelayCommand]
    public void RemoveMeal() => Meal = null;

    [RelayCommand]
    public void RemoveDrink() => Drink = null;
}
