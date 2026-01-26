using System.Globalization;
using Avalonia.Data.Converters;

namespace ExpenseTracker.UI.Converters;

public sealed class PercentToWidthConverter : IMultiValueConverter
{
    // values[0] = percent (0..1)
    // values[1] = available width
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return 0d;

        var percent = ToDouble(values[0]);
        var width = ToDouble(values[1]);

        if (double.IsNaN(percent) || double.IsNaN(width)) return 0d;

        percent = Math.Clamp(percent, 0d, 1d);
        return width * percent;
    }

    private static double ToDouble(object? o)
    {
        return o switch
        {
            null => double.NaN,
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal m => (double)m,
            string s when double.TryParse(s, out var d) => d,
            _ => double.NaN
        };
    }
}