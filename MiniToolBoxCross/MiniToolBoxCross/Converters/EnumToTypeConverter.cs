using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MiniToolBoxCross.Converters;

/// <summary>
/// 将枚举值转换为对应的 Type 对象
/// </summary>
public class EnumToTypeConverter : IValueConverter
{
    public static readonly EnumToTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (value is Enum enumValue)
            return enumValue.GetType();

        if (value is Type type)
            return type;

        return null;
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
