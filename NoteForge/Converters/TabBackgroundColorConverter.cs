using System;
using Microsoft.UI.Xaml;

namespace NoteForge.Converters;

public partial class TabBackgroundColorConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, string language)
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
}