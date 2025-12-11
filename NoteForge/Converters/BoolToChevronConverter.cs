using System;

namespace NoteForge.Converters;

public partial class BoolToChevronConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool isExpanded 
            ? isExpanded ? "\uE70D" : "\uE76C" 
            : "\uE76C";
    }
}
