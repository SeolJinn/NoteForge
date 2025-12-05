using System;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace NoteForge.Converters;

public partial class SidebarBackgroundColorConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool isSelected && isSelected
            ? new SolidColorBrush(Color.FromArgb(255, 52, 52, 52))
            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }
}