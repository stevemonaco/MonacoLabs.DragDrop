using Avalonia.Data.Converters;
using System.Linq;

namespace DragDropSample.Converters;

public static class AppConverters
{
    public static IValueConverter DislikedWorkers { get; } = new DislikedWorkersConverter();

    public static IMultiValueConverter FirstNonEmpty { get; } =
        new FuncMultiValueConverter<string?, string?>(values => values.FirstOrDefault(x => !string.IsNullOrEmpty(x)));
}
