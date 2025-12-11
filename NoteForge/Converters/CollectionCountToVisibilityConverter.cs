using System;
using System.Collections;
using Microsoft.UI.Xaml;

namespace NoteForge.Converters;

public partial class CollectionCountToVisibilityConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is ICollection collection 
            ? collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed 
            : Visibility.Collapsed;
    }
}
