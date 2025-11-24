using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace NoteForge.Converters;

public partial class TabBackgroundColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isActive)
        {
            var resourceKey = isActive ? "AppBackground" : "TopBar";
            if (Application.Current.Resources.TryGetValue(resourceKey, out var resource))
            {
                return resource;
            }
        }

        return Application.Current.Resources["TopBar"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

