using System;
using Microsoft.UI.Xaml.Data;

namespace NoteForge.Converters;

public abstract class ValueConverterBase : IValueConverter
{
    public abstract object Convert(object value, Type targetType, object parameter, string language);

    public virtual object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}