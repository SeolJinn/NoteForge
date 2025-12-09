using System;
using System.Collections;
using Microsoft.UI.Xaml;

namespace NoteForge.Converters;

public class IsLastItemConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is IList list && value != null)
        {
            var index = list.IndexOf(value);
            return index == list.Count - 1 ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }
}
