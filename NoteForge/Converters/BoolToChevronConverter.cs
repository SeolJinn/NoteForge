using System;
using Microsoft.UI.Xaml.Data;

namespace NoteForge.Converters;

public class BoolToChevronConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "\uE70D" : "\uE76C";
        }
        return "\uE76C";
    }
}
