using System;
using Microsoft.UI.Xaml.Data;

namespace NoteForge.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
            return $"{(int)(d * 100)}%";
        if (value is float f)
            return $"{(int)(f * 100)}%";
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
