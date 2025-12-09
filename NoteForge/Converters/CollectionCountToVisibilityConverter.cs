using System;
using System.Collections;
using Microsoft.UI.Xaml;

namespace NoteForge.Converters;

public class CollectionCountToVisibilityConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ICollection collection)
        {
            return collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }
}
