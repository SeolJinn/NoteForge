using System;
using Microsoft.UI.Xaml;

namespace NoteForge.Converters;

public partial class BoolToVisibilityConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue 
            ? boolValue ? Visibility.Visible : Visibility.Collapsed 
            : Visibility.Collapsed;
    }
}
