using System;
using Microsoft.UI.Xaml;

namespace NoteForge.Converters;

public class InverseBoolToVisibilityConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }
}
