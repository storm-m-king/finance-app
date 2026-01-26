using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ExpenseTracker.UI.Converters;

public sealed class ResourceBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrWhiteSpace(key))
            return Brushes.Transparent;

        if (Application.Current?.Resources.TryGetResource(key, theme: null, out var res) != true)
            return Brushes.Transparent;

        // If the resource is already a brush, use it.
        if (res is IBrush b)
            return b;

        // If someone stored a Color instead, wrap it.
        if (res is Color c)
            return new SolidColorBrush(c);

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}