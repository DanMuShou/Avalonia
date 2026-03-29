using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MiniToolBoxCross.Converters;

public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
            return value;

        var parts = paramString.Split(',');
        if (parts.Length == 2)
            return boolValue ? parts[0].Trim() : parts[1].Trim();

        return value;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotImplementedException();
    }
}
