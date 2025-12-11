using System;
using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace NoteForge.Converters;

public partial class ExpandedAndHasItemsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool isExpanded && parameter is ICollection collection
            ? isExpanded && collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
