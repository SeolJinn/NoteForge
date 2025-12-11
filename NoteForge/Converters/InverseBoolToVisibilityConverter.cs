using System;
using Microsoft.UI.Xaml;

namespace NoteForge.Converters;

public partial class InverseBoolToVisibilityConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue 
            ? boolValue ? Visibility.Collapsed : Visibility.Visible 
            : Visibility.Visible;
    }
}
